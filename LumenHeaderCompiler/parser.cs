namespace lhc;

internal struct QualifierArgs {

    public string? mDisplayName;
    public string? mParseName;
    public string? mCategoryName;
    public string? mMinVal;
    public string? mMaxVal;
    public string? mDragSpeed;

};
internal record FieldInfo( string mType, QualifierArgs mArgs, string mName );
internal record ClassInfo( string mTypeName, QualifierArgs mArgs, List<FieldInfo> mFields );

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

        if (mCurrent.mType == TokenType.LAngle) {
            while (mCurrent.mType != TokenType.RAngle) {
                increment( );
            }
            increment( );
        }

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

        QualifierArgs args = read_property_args( );

        expect( TokenType.RParen );

        if (mComponents.Count == 0)
            throw new Exception( $"LPROPERTY found before any LCLASS in {mCurrent}" );

        string type = parse_type( );
        string name = parse_name( );

        while (mCurrent.mType != TokenType.Semicolon)
            increment( );
        increment( );

        mComponents.Last( ).mFields.Add( new FieldInfo( type, args, name ) );

    }

    private QualifierArgs read_property_args( ) {

        QualifierArgs args = new( );

        while (mCurrent.mType != TokenType.RParen) {

            if (mCurrent.mType == TokenType.Identifier) {

                if (mCurrent.mValue.ToLower( ) == "displayname") {

                    increment( );
                    expect( TokenType.Equals );
                    args.mDisplayName = expect( TokenType.String ).mValue;

                }
                else if (mCurrent.mValue.ToLower( ) == "minval") {

                    increment( );
                    expect( TokenType.Equals );
                    args.mMinVal = expect( TokenType.Number ).mValue;

                }
                else if (mCurrent.mValue.ToLower( ) == "maxval") {

                    increment( );
                    expect( TokenType.Equals );
                    args.mMaxVal = expect( TokenType.Number ).mValue;

                }
                else if (mCurrent.mValue.ToLower( ) == "dragspeed") {

                    increment( );
                    expect( TokenType.Equals );
                    args.mDragSpeed = expect( TokenType.Number ).mValue;

                }
                else increment( );


            }
            else increment( );

        }

        return args;

    }

    private QualifierArgs read_class_args( ) {

        QualifierArgs args = new( );

        while (mCurrent.mType != TokenType.RParen) {

            if (mCurrent.mType == TokenType.Identifier) {

                if (mCurrent.mValue.ToLower( ) == "displayname") {
                    
                    increment( );
                    expect( TokenType.Equals );
                    args.mDisplayName = expect( TokenType.String ).mValue;
                    
                }
                else if(mCurrent.mValue.ToLower() == "parsename") {

                    increment( );
                    expect( TokenType.Equals );
                    args.mParseName = expect( TokenType.String ).mValue;

                }
                else if(mCurrent.mValue.ToLower() == "category") {

                    increment( );
                    expect( TokenType.Equals );
                    args.mCategoryName = expect( TokenType.String ).mValue;

                }
                else increment( );


            }
            else increment( );

        }

        return args;

    }

    private void parse_class( ) {

        expect( TokenType.Macro );
        expect( TokenType.LParen );

        QualifierArgs args = read_class_args( );

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
