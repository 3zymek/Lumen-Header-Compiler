namespace lhc;

class Program {

    static void Main( string[] args ) {

        Tokenizer tokenizer = new( );
        tokenizer.Tokenize( args[0] );

        Parser parser = new( tokenizer );
        parser.Parse( );

        foreach(PropertyData data in parser.mProperties) {
            Console.WriteLine( "Type = " + data.mType + "    " );
            Console.WriteLine( "Value = " + data.mValue + '\n' );
        }


    }

}

