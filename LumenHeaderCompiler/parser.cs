namespace lhc;

internal struct ClassArguments {

    public string? mID;

};
internal record FieldInfo( string mType, string mName );
internal record ClassInfo( string mTypeName, ClassArguments mArgs, List<FieldInfo> mFields );

internal class Parser {

    public readonly List<ClassInfo> mComponents = new( );

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
        return expect( TokenType.Identifier ).mValue;
    }

    private string parse_type( ) {

        string type = expect( TokenType.Identifier ).mValue;

        while (mCurrent.mType == TokenType.Colon) {
            increment( );
            if (mCurrent.mType == TokenType.Colon)
                increment( );
            type += "::" + expect( TokenType.Identifier ).mValue;
        }

        return type;

    }

    private void parse_property( ) {

        expect( TokenType.Macro );
        expect( TokenType.LParen );
        expect( TokenType.RParen );

        if (mComponents.Count == 0)
            throw new Exception( $"LPROPERTY found before any LCLASS in {mCurrent}" );

        string type = parse_type( );
        string name = parse_name( );

        mComponents.Last( ).mFields.Add( new FieldInfo( type, name ) );

    }

    private ClassArguments read_class_args( ) {

        ClassArguments args = new( );

        while (mCurrent.mType != TokenType.RParen) {

            if (mCurrent.mType == TokenType.Identifier) {

                if (mCurrent.mValue == "id") {

                    increment( );
                    expect( TokenType.Equals );
                    args.mID = expect( TokenType.String ).mValue;

                }

            }
            else increment( );

        }

        return args;

    }

    private void parse_class( ) {

        expect( TokenType.Macro );
        expect( TokenType.LParen );

        ClassArguments args = read_class_args( );

        expect( TokenType.RParen );

        string keyword = expect( TokenType.Keyword ).mValue;
        string name = expect( TokenType.Identifier ).mValue;

        mComponents.Add( new ClassInfo( name, args, new( ) ) );

    }

    public void Parse( ) {

        mComponents.Clear( );
        mPosition = 0;

        while (mPosition < mTokens.Count) {

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
