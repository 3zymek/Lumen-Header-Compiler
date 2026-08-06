using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class SerializeRegistry : IRegistry {

    private readonly ConfigFile mCfg;

    public SerializeRegistry( ConfigFile cfg ) {

        mCfg = cfg;

    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

        // BLANK

    }
    public void Finalize( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        foreach (var info in classInfos) {
            
            
            
        }

    }

}
