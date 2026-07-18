using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace lhc;

internal enum TokenType {
    Macro,
    Identifier,
    Keyword,
    LParen,
    RParen,
    LBracket,
    RBracket,
    Semicolon,
    Comma,
    Equals,
    Colon,
    String,
    Number,
    LAngle,
    RAngle
}
internal record Token(
    TokenType mType,
    string mValue,
    int mLine,
    string mFile
    );

internal class TokenizerMacros {
    public string class_macro { get; init; } = "";
    public string property_macro { get; init; } = "";
    public string function_macro { get; init; } = "";
    public string generated_body_macro { get; init; } = "";
    public string class_extensions_macro { get; init; } = "";

    [JsonIgnore]
    public List<string> mAll => [
        class_macro,
        property_macro,
        function_macro,
        generated_body_macro,
        class_extensions_macro
    ];

}

internal record TokenizerConfigFile(
    TokenizerMacros macros,
    List<string> tokens_to_ignore,
    List<string> ends_with_to_ignore,
    List<string> starts_with_to_ignore
    );

internal class Tokenizer {

    public readonly List<Token> mTokens = new( );
    private readonly TokenizerConfigFile mCfg;
    private readonly List<string> mRegisteredKeywords = new( ) {
        "class", "struct", "private", "protected", "public", "namespace"
    };
    private int mCurrentIndex = 0;
    private int mCurrentLine = 1;
    private string mFileName = "";
    private string mFileContent = "";

    public Tokenizer( TokenizerConfigFile cfg ) {
        mCfg = cfg;
    }

    public void Tokenize( string filename ) {

        mTokens.Clear( );

        if (!File.Exists( filename )) {
            throw new FileNotFoundException( $"File {filename} doesn't exist." );
        }

        mFileName = filename;
        mFileContent = File.ReadAllText( filename );
        mCurrentIndex = 0;
        mCurrentLine = 1;

        while (!is_at_end( )) {

            char c = advance( );

            if (char.IsWhiteSpace( c )) continue;
            if (c == '\n') { mCurrentLine++; continue; }

            switch (c) {
                case '/' when peek( ) == '*': handle_block_comment( ); break;
                case '/' when peek( ) == '/': skip_line( ); break;
                case '#': skip_line( ); break;
                case '<': mTokens.Add( new( TokenType.LAngle, "<", mCurrentLine, filename ) ); break;
                case '>': mTokens.Add( new( TokenType.RAngle, ">", mCurrentLine, filename ) ); break;
                case '{': mTokens.Add( new( TokenType.LBracket, "{", mCurrentLine, filename ) ); break;
                case '}': mTokens.Add( new( TokenType.RBracket, "}", mCurrentLine, filename ) ); break;
                case '(': mTokens.Add( new( TokenType.LParen, "(", mCurrentLine, filename ) ); break;
                case ')': mTokens.Add( new( TokenType.RParen, ")", mCurrentLine, filename ) ); break;
                case ';': mTokens.Add( new( TokenType.Semicolon, ";", mCurrentLine, filename ) ); break;
                case ':': mTokens.Add( new( TokenType.Colon, ":", mCurrentLine, filename ) ); break;
                case '=': mTokens.Add( new( TokenType.Equals, "=", mCurrentLine, filename ) ); break;
                case ',': mTokens.Add( new( TokenType.Comma, ",", mCurrentLine, filename ) ); break;
                case '"': read_string_literal( ); break;

                case char _ when char.IsLetter( c ) || c == '_':
                    read_identifier_or_keyword( c ); break;
                case char _ when (char.IsDigit( c ) || (c == '-' && char.IsDigit( peek( ) ))):
                    read_number( c ); break;
                default:
                    break;
            }

        }

    }

    private char advance( ) => mFileContent[mCurrentIndex++];
    private char peek( int offset = 0 ) => (mCurrentIndex + offset < mFileContent.Length) ? mFileContent[mCurrentIndex + offset] : '\0';
    private bool is_at_end( ) => mCurrentIndex >= mFileContent.Length;

    private void skip_line( ) {
        while (!is_at_end( ) && peek( ) != '\n')
            advance( );
    }

    private void handle_block_comment( ) {
        advance( );
        while (!is_at_end( ) && !(peek( ) == '*' && peek( 1 ) == '/')) {
            if (peek( ) == '\n') mCurrentLine++;
            advance( );
        }

        if (!is_at_end( )) advance( );
        if (!is_at_end( )) advance( );
    }

    private void read_string_literal( ) {
        string value = "";
        while (!is_at_end( ) && peek( ) != '"') {
            value += advance( );
        }
        advance( );
        mTokens.Add( new( TokenType.String, value, mCurrentLine, mFileName ) );
    }

    private void read_identifier_or_keyword( char startChar ) {
        string value = startChar.ToString( );
        while (!is_at_end( ) && (char.IsLetterOrDigit( peek( ) ) || peek( ) == '_'))
            value += advance( );

        if (should_ignore( value )) return;

        if (mCfg.macros.mAll.Any( macro => value == macro )) {
            mTokens.Add( new( TokenType.Macro, value, mCurrentLine, mFileName ) );
        }
        else if (mRegisteredKeywords.Contains( value )) {

            Token? lastToken = mTokens.LastOrDefault( );

            if (lastToken != null && lastToken.mType == TokenType.Colon) {
                return;
            }
            else {
                mTokens.Add( new( TokenType.Keyword, value, mCurrentLine, mFileName ) );
            }

        }
        else {
            mTokens.Add( new( TokenType.Identifier, value, mCurrentLine, mFileName ) );
        }

    }

    private void read_number( char startChar ) {
        string value = startChar.ToString( );

        if (peek( ) == '-') value += advance( );

        while (!is_at_end( ) && (char.IsDigit( peek( ) ) || peek( ) == '.'))
            value += advance( );

        if (!is_at_end( ) && (peek( ) == 'f' || peek( ) == 'u' || peek( ) == 'l' || peek( ) == 'L'))
            advance( );

        mTokens.Add( new( TokenType.Number, value, mCurrentLine, mFileName ) );
    }

    private bool should_ignore( string value ) {
        if (mCfg.tokens_to_ignore.Any( token => value == token ))
            return true;
        else if (mCfg.starts_with_to_ignore.Any( prefix => value.StartsWith( prefix ) ))
            return true;
        else if (mCfg.ends_with_to_ignore.Any( suffix => value.EndsWith( suffix ) ))
            return true;
        return false;
    }

}