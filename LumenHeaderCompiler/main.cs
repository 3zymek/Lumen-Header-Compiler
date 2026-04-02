namespace lhc;

internal class Program {

    static void Main( string[] args ) {

        string inputDir = args[0] ?? throw new Exception( "Invalid dotnet argument, missing input dir" );
        string sceneParserPath = Path.Combine( inputDir, "engine/include/modules/scene/format/scene_parser.hpp" );

        var files = Directory.GetFiles( inputDir, "*.hpp", SearchOption.AllDirectories ).
            Where(f => !f.Contains(Path.Combine(inputDir, "external")) && !f.Contains("internal_assets"));

        Tokenizer tokenizer = new( );
        Parser parser = new( tokenizer );

        foreach (var file in files) {

            tokenizer.Tokenize( file.ToString( ) );
            parser.Parse( );

            if(parser.mProperties.Count > 0) {
                HeaderGenerator.Generate( file, parser.mProperties );
            }

        }

        HeaderGenerator.Finalize( sceneParserPath );

    }

}

