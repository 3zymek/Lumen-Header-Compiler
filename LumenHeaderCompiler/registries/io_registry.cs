using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class IoRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    public IoRegistry( ConfigFile cfg ) {
        mCfg = cfg;
    }

    public void GenerateFile( string sourceFile, ClassInfo info ) {

        /*
            TODO: create ParseHelper and SerializeHelper and use IoRegistry as bridge between parsing and serializing
            one file for both functions AND ADD COMPONENT NAME GETTERS FROM LhcPipeline ~03.07.2026
        */
        
    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

    }
}

