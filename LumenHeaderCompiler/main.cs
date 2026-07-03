using System.Text.Json;

namespace lhc;

internal record TypeProperties( 
    string reader, 
    List<string> inspector, 
    List<string>? droppable_inspector
    );
internal record OutputProperties(
    string registry_type,
    string output_path,
    string base_include,
    string template_function,
    string function_namespace
    );
internal record ConfigFile(
    List<OutputProperties> outputs,
    Dictionary<string, string> paths,
    Dictionary<string, string> category_colors,
    Dictionary<string, string> category_icons,
    List<string> prefixes,
    Dictionary<string, List<string>> function_templates,
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

        JsonSerializerOptions options = new( ) {
            PropertyNameCaseInsensitive = true
        };

        string jsonContent = File.ReadAllText( $"{Path.Combine( AppContext.BaseDirectory, "config.json" )}" );
        ConfigFile config = JsonSerializer.Deserialize<ConfigFile>( jsonContent, options ) ??
           throw new Exception( $"Failed to deserialize {Path.Combine( AppContext.BaseDirectory, "config.json" )}" );

        Tokenizer tokenizer = new( );
        Parser parser = new( tokenizer );

        LhcPipeline lhcPipeline = new( rootDir, config );

        foreach (var file in files) {

            tokenizer.Tokenize( file.ToString( ) );
            Console.WriteLine( $"Parsing: {file}" );
            parser.Parse( );

            if (parser.mComponents.Count > 0) {
                lhcPipeline.GenerateFile( file, parser.mComponents );
            }

        }

        lhcPipeline.Finalize( );

    }

}

