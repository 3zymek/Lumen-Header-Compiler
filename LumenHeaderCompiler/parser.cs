namespace lhc;

record PropertyData( string mType, string mValue );

class Parser {

    public readonly List<PropertyData> mProperties = new( );

    private List<Token> mTokens;
    private int mPosition = 0;

    private Token mCurrent => mTokens[mPosition];
    private Token increment( ) => mTokens[mPosition++];
    private Token preincrement( ) => mTokens[++mPosition];

    public Parser( Tokenizer tokenizer ) { mTokens = tokenizer.mTokens; }

    private Token expect( TokenType type ) {
        if (type != mCurrent.mType) {
            throw new Exception( $"Expected {type} but got {mCurrent.mType}" );
        }
        return increment( );
    }


    private string parse_name( ) {
        Console.Write( mCurrent.mValue );
        return expect( TokenType.Identifier ).mValue;
    }

    private string parse_type( ) {

        string type = expect( TokenType.Identifier ).mValue;

        while(mCurrent.mType == TokenType.Colon) {
            increment( );
            increment( );
            type += "::" + expect( TokenType.Identifier ).mValue;
        }
        
        return type;

    }

    private void parse_property( ) {

        expect( TokenType.Macro );
        expect( TokenType.LParen );
        expect( TokenType.RParen );

        string type = parse_type( );
        string name = parse_name( );

        mProperties.Add( new( type, name ) );
            
    }

    private void parse_class( ) {

        expect( TokenType.Macro );
        expect( TokenType.LParen );
        expect( TokenType.RParen );

        string keyword = expect( TokenType.Keyword ).mValue;

        mProperties.Add( new( keyword, keyword ) );

    }

    public void Parse( ) {

        while(mPosition < mTokens.Count) {

            if (mCurrent.mType == TokenType.Macro) {

                if (mCurrent.mValue == "LCLASS") {
                    parse_class( );
                }
                else if (mCurrent.mValue == "LPROPERTY") {
                    parse_property( );
                }

            }
            else increment( );

        }

    }

}
