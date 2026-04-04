namespace lhc;

internal class Program {

    static void Main( string[] args ) {

        string inputDir = args[0] ?? throw new Exception( "Invalid dotnet argument, missing input dir" );
        string sceneDepMgr = Path.Combine( inputDir, args[1] ?? throw new Exception( "Missing scene dependency manager relative path from dotnet args" ) );

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

        HeaderGenerator.Finalize( sceneDepMgr );


    }

}

