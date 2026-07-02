using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal interface IRegistry {
    void Finalize( string rootDir, Dictionary<string, ClassGeneratedInfo> classInfos, OutputProperties outProps );

}
