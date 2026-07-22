using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal interface IRegistry {
    void HandleFile( string sourceFile, ClassInfo info );
    void Finalize( List<ClassGeneratedInfo> classInfos, OutputProperties outProps );
}
