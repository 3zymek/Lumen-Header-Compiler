using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class EcsTraitsRegistry : IRegistry {

    ConfigFile mCfg;

    public EcsTraitsRegistry( ConfigFile cfg ) {
        mCfg = cfg;
    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        StringBuilder sb = new( );

        string preamble = mCfg.ResolveFilePreamble( null );
        sb.AppendLine( preamble );

        string baseFileBlueprint = string.Join( '\n', mCfg.GetBlueprint( "ecs_traits_basefile" ) );


    }

    private void inject_parse_traits( string blueprint ) {

        int index = blueprint.FindTokenIndex( "ecs_traits_basefile", "{ParseNameTraits}" );
        string alignment = new( "" );
        for (int i = index - 1; i >= 0; i--) {

            

        }



    }
    private void inject_display_traits( ) {

    }
    private void inject_category_traits( ) {

    }

}
