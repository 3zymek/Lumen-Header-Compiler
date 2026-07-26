using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal sealed class Blueprint {

    public string mKey { get; init; }
    public string mContent { get; init; }

    public Blueprint( string key, string content ) {
        mKey = key;
        mContent = content;
    }

}