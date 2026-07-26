using System;
using System.Collections.Generic;
using System.Text;
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

internal class TokenizerConfigFile {
    public required List<string> tokens_to_ignore { get; init; }
    public required List<string> ends_with_to_ignore { get; init; }
    public required List<string> starts_with_to_ignore { get; init; }
    [JsonIgnore] public SupportedMacros mMacros { get; set; }

}