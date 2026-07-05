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

internal class Tokenizer {

    public readonly List<Token> mTokens = new( );

    public void Tokenize( string filename ) {

        mTokens.Clear( );

        if (!File.Exists( filename )) {
            throw new Exception( $"File {filename} doesn't exist" );
        }

        string content = File.ReadAllText( filename );

        int currentLine = 1;
        int i = 0;
        while (i < content.Length) {

            char c = content[i];

            if (char.IsWhiteSpace( c )) { i++; continue; }
            if (c == '\n') { currentLine++; continue; }

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

            else if (c == '#') {
                while (i < content.Length && content[i] != '\n')
                    i++;
                continue;
            }
            else if (char.IsLetter( c ) || c == '_') {

                string value = "";

                while (i < content.Length && (char.IsLetterOrDigit( content[i] ) || content[i] == '_'))
                    value += content[i++];

                if (value == "LCLASS") {
                    mTokens.Add( new( TokenType.Macro, value, currentLine, filename ) );
                }
                else if (value == "LPROPERTY") {
                    mTokens.Add( new( TokenType.Macro, value, currentLine, filename ) );
                }
                else if (value == "class" || value == "struct") {
                    mTokens.Add( new( TokenType.Keyword, value, currentLine, filename ) );
                }
                else mTokens.Add( new( TokenType.Identifier, value, currentLine, filename ) );

            }
            else if (char.IsDigit( c ) || (c == '-' && i + 1 < content.Length && char.IsDigit( content[i + 1] ))) {

                string value = "";

                if (c == '-') value += content[i++];

                while (i < content.Length && (char.IsDigit( content[i] ) || content[i] == '.'))
                    value += content[i++];

                if (i < content.Length && (content[i] == 'f' || content[i] == 'u' || content[i] == 'l' || content[i] == 'L'))
                    i++;

                mTokens.Add( new( TokenType.Number, value, currentLine, filename ) );

            }
            else if (c == '"') {
                i++;
                string value = "";
                while (i < content.Length && content[i] != '"') {
                    value += content[i++];
                }
                i++;
                mTokens.Add( new( TokenType.String, value, currentLine, filename ) );
            }
            else if (c == '<') { mTokens.Add( new( TokenType.LAngle, "<", currentLine, filename ) ); i++; }
            else if (c == '>') { mTokens.Add( new( TokenType.RAngle, ">", currentLine, filename ) ); i++; }
            else if (c == '{') { mTokens.Add( new( TokenType.LBracket, "{", currentLine, filename ) ); i++; }
            else if (c == '}') { mTokens.Add( new( TokenType.RBracket, "}", currentLine, filename ) ); i++; }
            else if (c == '(') { mTokens.Add( new( TokenType.LParen, "(", currentLine, filename ) ); i++; }
            else if (c == ')') { mTokens.Add( new( TokenType.RParen, ")", currentLine, filename ) ); i++; }
            else if (c == ';') { mTokens.Add( new( TokenType.Semicolon, ";", currentLine, filename ) ); i++; }
            else if (c == ':') { mTokens.Add( new( TokenType.Colon, ":", currentLine, filename ) ); i++; }
            else if (c == '=') { mTokens.Add( new( TokenType.Equals, "=", currentLine, filename ) ); i++; }
            else if (c == ',') { mTokens.Add( new( TokenType.Comma, ",", currentLine, filename ) ); i++; }
            else i++;

        }

    }


}