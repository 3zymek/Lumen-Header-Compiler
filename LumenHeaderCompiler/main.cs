using System.Text.Json;

namespace lhc;

internal record TypeProperties( 
    string reader, 
    List<string> inspector, 
    List<string>? droppable_inspector
    );
internal record OutputProperties(
    string registry_type,
    string finalize_path
    );
internal record ConfigFile(
    List<OutputProperties> outputs,
    Dictionary<string, string> category_colors,
    Dictionary<string, string> category_icons,
    List<string> prefixes,
    Dictionary<string, List<string>> blueprints,
    Dictionary<string, string> templates,
    Dictionary<string, string> defaults,
    Dictionary<string, TypeProperties> types
    );

internal class Program {

    static void Main( string[] args ) {

        JsonSerializerOptions serializerOptions = new( ) {
            PropertyNameCaseInsensitive = true
        };

        ConfigFile          baseConfig      = ConfigLoader.LoadFromFile<ConfigFile>( "config.json", serializerOptions );
        TokenizerConfigFile tokenizerConfig = ConfigLoader.LoadFromFile<TokenizerConfigFile>( "TokenizerConfig.json", serializerOptions );

        Tokenizer tokenizer = new( tokenizerConfig );
        Parser parser = new( tokenizer );

        string rootDir = args[0] ?? throw new Exception( "Invalid dotnet argument, missing root dir" );
        var files = Directory.GetFiles( rootDir, "*.hpp", SearchOption.AllDirectories )
            .Where( f => !f.Contains( Path.Combine( rootDir, "External" ) ) )
            .Where( f => !f.EndsWith( ".generated.hpp" ) );

        tokenizer.Tokenize( Path.Combine( AppContext.BaseDirectory, "test.hpp" ) );
        foreach(var token in tokenizer.mTokens) {
            Console.WriteLine( $"{token.mValue} = {token.mType}" );
        }


       /*
        LhcPipeline lhcPipeline = new( rootDir, baseConfig );

        foreach (var file in files) {

            tokenizer.Tokenize( file.ToString( ) );
            Console.WriteLine( $"Parsing: {file}" );
            parser.Parse( );

            if (parser.mClassInfos.Count > 0) {
                lhcPipeline.HandleFile( file, parser.mClassInfos );
            }

        }

        lhcPipeline.Finalize( );
       */

    }

}

