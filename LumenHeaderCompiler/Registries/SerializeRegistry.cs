using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class SerializeRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private SerializeHelper mSerializeHelper;

    public SerializeRegistry( ConfigFile cfg ) {

        mCfg = cfg;
        mSerializeHelper = new( mCfg );

    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

        // BLANK

    }
    public void Finalize( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        StringBuilder sb = new( );
        foreach (var info in classInfos) {

            sb.AppendLine( mSerializeHelper.BuildSerializeFunction( info.mInfo ) );
            sb.AppendLine( "FIUT" );
            
        }
        
        string finalizePath = Path.Combine( LhcPipeline.mRootDir, outProps.finalize_path ).MakeGeneratedPath( "hpp" );
        File.WriteAllText( finalizePath, sb.ToString( ) );

    }

}
