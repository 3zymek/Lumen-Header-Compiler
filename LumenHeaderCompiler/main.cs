namespace lhc;

internal class Program {

    static void Main( string[] args ) {

        Tokenizer tokenizer = new( );
        tokenizer.Tokenize( args[0] );

        Parser parser = new( tokenizer );
        parser.Parse( );

        HeaderGenerator.Generate( args[0], parser.mProperties );


    }

}

