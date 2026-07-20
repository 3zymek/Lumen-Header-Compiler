using System.Text.Json.Serialization;

namespace lhc;

internal class Parser {

    public readonly List<ClassInfo> mClassInfos = new( );
    private readonly ParserConfigFile mCfg;
    private readonly List<ParserMacroConfig> mMacroHandlers;

    private List<Token> mTokens;
    private string mCurrentNamespace = "";
    private int mPosition = 0;

    public Parser( ParserConfigFile cfg, List<Token> tokens ) {

        mTokens = tokens;
        mCfg = cfg;
        mMacroHandlers = new( )
        {
            new ParserMacroConfig()
            {
                mParseFn = parse_property,
                mValue = mCfg.mSupportedMacros.property_macro
            },
            new ParserMacroConfig()
            {
                mParseFn = parse_class,
                mValue = mCfg.mSupportedMacros.class_macro
            }

        };

    }

    public void Parse( ) {

        mClassInfos.Clear( );
        mPosition = 0;

        while (mPosition < mTokens.Count) {

            if (peek( ).mType == TokenType.Macro) {

                bool handled = false;
                foreach(var handler in mMacroHandlers) {
                    if (peek( ).mValue == handler.mValue) {
                        handler.mParseFn( );
                        handled = true;
                        break;
                    }
                }
                if (!handled)
                    advance( );

            }
            else advance( );

        }

    }
    private Token peek( int offset = 0 ) => (mPosition + offset < mTokens.Count)
        ? mTokens[mPosition + offset]
        : throw new IndexOutOfRangeException( "Tried to peek beyond the end of the token stream." );

    private Token advance( ) => mTokens[mPosition++];

    private Token expect( TokenType type ) {
        if (type != peek( ).mType) {
            throw new LhcException( $"Expected \"{type}\" but got \"{peek( ).mType}\"", peek( ).mFile, peek( ).mLine );
        }
        return advance( );
    }
    private Token expect( string value ) {
        if (value != peek( ).mValue) {
            throw new LhcException( $"Expected \"{value}\" but got \"{peek( ).mValue}\"", peek( ).mFile, peek( ).mLine );
        }
        return advance( );
    }


    private string parse_name( ) {
        return expect( TokenType.Identifier ).mValue;
    }

    private string parse_type( ) {

        string type = expect( TokenType.Identifier ).mValue;

        if (peek( ).mType == TokenType.LAngle) {
            while (peek( ).mType != TokenType.RAngle) {
                advance( );
            }
            advance( );
        }

        while (peek( ).mType == TokenType.Colon) {
            advance( );
            if (peek( ).mType == TokenType.Colon)
                advance( );
            type += "::" + expect( TokenType.Identifier ).mValue;
        }

        return type;

    }

    private void parse_class( ) {

        expect( TokenType.Macro );
        expect( TokenType.LParen );

        QualifierArgs args = read_class_args( );

        expect( TokenType.RParen );

        string keyword = expect( TokenType.Keyword ).mValue;
        string name = expect( TokenType.Identifier ).mValue;

        mClassInfos.Add( new ClassInfo( name, args, new( ) ) );

    }

    private void parse_property( ) {

        expect( TokenType.Macro );
        expect( TokenType.LParen );

        QualifierArgs args = read_property_args( );

        expect( TokenType.RParen );

        if (mClassInfos.Count == 0)
            throw new LhcException( $"{mCfg.mSupportedMacros.property_macro} found before any {mCfg.mSupportedMacros.class_macro} in {peek( )}", peek( ).mFile, peek( ).mLine );

        string type = parse_type( );
        string name = parse_name( );

        while (peek( ).mType != TokenType.Semicolon)
            advance( );
        advance( );

        mClassInfos.Last( ).mFields.Add( new FieldInfo( type, args, name ) );

    }

    private QualifierArgs read_property_args( ) {

        QualifierArgs args = new( );

        while (peek( ).mType != TokenType.RParen) {

            if (peek( ).mType == TokenType.Identifier) {

                var matchedProp = mCfg.property_args.FirstOrDefault( prop => peek( ).mValue.ToLower( ) == prop.name );
                if (matchedProp != null) {

                    advance( );
                    expect( matchedProp.assignment_operator );
                    args.mProperties[matchedProp.name] = expect( matchedProp.expected_token ).mValue;

                }
                else advance( );


            }
            else advance( );

        }

        return args;

    }

    private QualifierArgs read_class_args( ) {

        QualifierArgs args = new( );

        while (peek( ).mType != TokenType.RParen) {

            if (peek( ).mType == TokenType.Identifier) {

                var matchedArg = mCfg.class_args.FirstOrDefault( prop => peek( ).mValue.ToLower( ) == prop.name );
                if(matchedArg != null) {

                    advance( );
                    expect( matchedArg.assignment_operator );
                    args.mProperties[matchedArg.name] = expect( matchedArg.expected_token ).mValue;

                }
                else advance( );

            }
            else advance( );

        }

        return args;

    }

}
