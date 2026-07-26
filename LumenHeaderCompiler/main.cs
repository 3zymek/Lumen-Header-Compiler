using System.Text.Json;
using System.Text.Json.Serialization;

namespace lhc;

internal class Program {

    static void Main( string[] args ) {

        JsonSerializerOptions serializerOptions = new( ) {
            PropertyNameCaseInsensitive = true
        };

        ConfigFile baseConfig = ConfigLoader.LoadFromFile<ConfigFile>( "Config.json", serializerOptions );
        baseConfig.mTypesCfg = ConfigLoader.LoadFromFile<TypesConfigFile>( "Types.json", serializerOptions );

        TokenizerConfigFile tokenizerConfig = ConfigLoader.LoadFromFile<TokenizerConfigFile>( "TokenizerConfig.json", serializerOptions );
        tokenizerConfig.mMacros = baseConfig.supported_macros;

        ParserConfigFile parserConfig = ConfigLoader.LoadFromFile<ParserConfigFile>( "ParserConfig.json", serializerOptions );
        parserConfig.mSupportedMacros = baseConfig.supported_macros;

        Tokenizer tokenizer = new( tokenizerConfig );
        Parser parser = new( parserConfig, tokenizer.mTokens );

        string rootDir = args[0] ?? throw new Exception( "Invalid dotnet argument, missing root dir" );
        var files = Directory.GetFiles( rootDir, "*.hpp", SearchOption.AllDirectories )
            .Where( f => !f.Contains( Path.Combine( rootDir, "External" ) ) )
            .Where( f => !f.EndsWith( ".gen.hpp" ) );

        LhcPipeline lhcPipeline = new( rootDir, baseConfig );

        foreach (var file in files) {

            tokenizer.Tokenize( file.ToString( ) );
            Console.WriteLine( $"Resolving: {Path.GetRelativePath(rootDir, file)}" );
            parser.Parse( );

            if (parser.mClassInfos.Count > 0) {
                lhcPipeline.HandleFile( file, parser.mClassInfos );
            }

        }

        lhcPipeline.Finalize( );
       

    }

}

