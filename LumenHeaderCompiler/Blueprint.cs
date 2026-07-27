using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal sealed class Blueprint {

    public string mKey { get; private set; }
    public string mContent { get; set; }

    public Blueprint( string key, string content ) {
        mKey = key;
        mContent = content;
    }

    public Blueprint FormatWith( string key, string value ) {
        mContent = mContent.FormatWith( key, value );
        return this;
    }
    public Blueprint FormatWith(Dictionary<string, string> formats) {
        mContent = mContent.FormatWith( formats );
        return this;
    }
    public int FindTokenIndex( string token ) {
        int index = mContent.IndexOf( token );
        if (index == -1) throw new ArgumentNullException( $"Couldn't find {token} token in {mKey}" );
        return index;
    }
    public string CalculateIndent( int baseIndex) {
        string alignment = string.Empty;
        for (int i = baseIndex - 1; i >= 0; i--) {
            char c = mContent[i];
            if (c == '\n' || c == '\r')
                break;
            if (char.IsWhiteSpace( c ))
                alignment = c + alignment;
            else break;
        }
        return alignment;
    }
    public Blueprint Replace( string key, string value ) {
        mContent = mContent.Replace( key, value );
        return this;
    }

}