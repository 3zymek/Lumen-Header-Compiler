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
    Colon,
}
internal record Token( TokenType mType, string mValue );

internal class Tokenizer {

    public readonly List<Token> mTokens = new( );

    public void Tokenize( string filename ) {

        if (!File.Exists( filename )) {
            throw new Exception( $"File {filename} doesn't exist" );
        }

        string content = File.ReadAllText( filename );

        int i = 0;
        while (i < content.Length) {

            char c = content[i];

            if (char.IsWhiteSpace( c )) { i++; continue; }

            else if (c == '/' && i + 1 < content.Length && content[i + 1] == '*') {
                i += 2;
                while (i + 1 < content.Length && !(content[i] == '*' && content[i + 1] == '/')) i++;
                i += 2;
                continue;
            }
            else if (c == '/' && i + 1 < content.Length && content[i + 1] == '/') {
                while (i < content.Length && content[i] != '\n')
                    i++;
                continue;
            }

            else if (char.IsLetter( c ) || c == '_') {

                string value = "";

                while (i < content.Length && (char.IsLetterOrDigit( content[i] ) || content[i] == '_'))
                    value += content[i++];

                if (value == "LCLASS") {
                    mTokens.Add( new( TokenType.Macro, value ) );
                }
                else if (value == "LPROPERTY") {
                    mTokens.Add( new( TokenType.Macro, value ) );
                }
                else if (value == "class" || value == "struct") {
                    mTokens.Add( new( TokenType.Keyword, value ) );
                }
                else mTokens.Add( new( TokenType.Identifier, value ) );

            }
            else if (c == '{') { mTokens.Add( new( TokenType.LBracket, "{" ) ); i++; }
            else if (c == '}') { mTokens.Add( new( TokenType.RBracket, "}" ) ); i++; }
            else if (c == '(') { mTokens.Add( new( TokenType.LParen, "(" ) ); i++; }
            else if (c == ')') { mTokens.Add( new( TokenType.RParen, ")" ) ); i++; }
            else if (c == ';') { mTokens.Add( new( TokenType.Semicolon, ";" ) ); i++; }
            else if (c == ':') { mTokens.Add( new( TokenType.Colon, ":" ) ); i++; }
            else i++;

        }

    }


}