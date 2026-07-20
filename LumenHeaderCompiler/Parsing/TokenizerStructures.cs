using System;
using System.Collections.Generic;
using System.Text;

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

internal class TokenizerConfigFile {
    public required SupportedMacros macros { get; init; }
    public required List<string> tokens_to_ignore { get; init; }
    public required List<string> ends_with_to_ignore { get; init; }
    public required List<string> starts_with_to_ignore { get; init; }
}