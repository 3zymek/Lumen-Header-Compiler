using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class IoRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    public IoRegistry( ConfigFile cfg ) {
        mCfg = cfg;
    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

        StringBuilder sb = new( );
        string preamble = mCfg.ResolveFilePreamble( sourceFile );
        sb.AppendLine( preamble );

        string parseFunctions = info.BuildParseFunctions( sourceFile, mCfg );
        sb.AppendLine( parseFunctions );


        /*
         * string serializeFunctions = info.BuildSerializeFunctions( sourceFile, mCfg );
         * sb.AppendLine( serializeFunctions );
         */

        /*
            TODO: create ParseHelper and SerializeHelper and use IoRegistry as bridge between parsing and serializing
            one file for both functions AND ADD COMPONENT NAME GETTERS FROM LhcPipeline ~03.07.2026
        */

        string generatedPath = sourceFile.MakeGeneratedHeaderPath( );
        File.WriteAllText( generatedPath, sb.ToString( ) );
    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        ParseHelper.FinalizeParseRegistry( rootDir, classInfos, outProps, mCfg );

    }
}

