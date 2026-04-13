
using System.Text.Json;

namespace lhc;

internal record TypeProperties( string reader, string inspector );
internal record ConfigFile(
    Dictionary<string, string> paths,
    List<string> prefixes,
    Dictionary<string, string> templates,
    Dictionary<string, string> defaults,
    Dictionary<string, TypeProperties> types
    );

internal class Program {

    static void Main( string[] args ) {

        string rootDir = args[0] ?? throw new Exception( "Invalid dotnet argument, missing root dir" );
        var files = Directory.GetFiles( rootDir, "*.hpp", SearchOption.AllDirectories )
            .Where( f => !f.Contains( Path.Combine( rootDir, "external" ) ) )
            .Where( f => !f.Contains( "internal_assets" ) )
            .Where( f => !f.EndsWith( ".generated.hpp" ) );

        string jsonContent = File.ReadAllText( $"{Path.Combine( AppContext.BaseDirectory, "config.json" )}" );
        ConfigFile config = JsonSerializer.Deserialize<ConfigFile>(jsonContent) ??
           throw new Exception( $"Failed to deserialize {Path.Combine( AppContext.BaseDirectory, "config.json" )}" );

        Tokenizer tokenizer = new( );
        Parser parser = new( tokenizer );

        HeaderGenerator.Initialize( rootDir, config );
        
        foreach (var file in files) {

            tokenizer.Tokenize( file.ToString( ) );
            Console.WriteLine( $"Parsing: {file}" );
            parser.Parse( );
            
            if (parser.mComponents.Count > 0) {
                HeaderGenerator.GenerateFile( file, parser.mComponents );
            }

        }

        HeaderGenerator.Finalize( );

    }

}

