using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal interface IRegistry {
    void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps );
    void GenerateFile( string sourceFile, ClassInfo info );
}
