namespace lhc;

internal class Program {

    static void Main( string[] args ) {

        string inputDir = args[0] ?? throw new Exception( "Invalid dotnet argument, missing input dir" );
        string sceneParserPath = Path.Combine( inputDir, /*"engine/include/modules/scene/format/scene_parser.hpp"*/ $"scene_parser.hpp" );

        var files = Directory.GetFiles( inputDir, "*.hpp", SearchOption.AllDirectories )
            .Where( f => !f.Contains( Path.Combine( inputDir, "external" ) ) )
            .Where( f => !f.Contains( "internal_assets" ) )
            .Where( f => !f.EndsWith( ".generated.hpp" ) );

        Tokenizer tokenizer = new( );
        Parser parser = new( tokenizer );

        HeaderGenerator.Initialize( );

        foreach (var file in files) {

            tokenizer.Tokenize( file.ToString( ) );
            parser.Parse( );

            if (parser.mComponents.Count > 0) {
                HeaderGenerator.GenerateFile( file, parser.mComponents );
            }

        }

        HeaderGenerator.Finalize( sceneParserPath );

    }

}

