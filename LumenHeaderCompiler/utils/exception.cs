using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class LhcException : Exception {

    public string mSourceFile { get; }
    public int mLine { get;  }

    public LhcException( string message, string sourceFile, int line ) : base( $"{message} (File: {sourceFile}, Line: {line}" ) {
        mSourceFile = sourceFile;
        mLine = line;
    }

}
