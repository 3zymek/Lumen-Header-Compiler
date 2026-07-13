using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal enum EEditorTraitType {
    CategoryColor,
    CategoryIcon
}

internal class EditorTraitConfig {

    public Func<EditorTraitConfig, string> mInjectFn = ( n ) => { Console.WriteLine( "mInjectFn in EditorTraitConfig has none function established" ); return ""; };
    public string mToken { get; set; } = "";
    public string mBlueprintName { get; set; } = "";

}

internal class EditorTraitsRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private readonly Dictionary<EEditorTraitType, EditorTraitConfig> mTraitToConfig;

    public EditorTraitsRegistry( ConfigFile cfg ) {
        mCfg = cfg;
        mTraitToConfig = new()
        {
            {
                EEditorTraitType.CategoryIcon, new EditorTraitConfig
                {
                    mToken = "{CategoryIconTraits}",
                    mBlueprintName = "ecs_trait_category_icon",
                    mInjectFn = inject_category_icons_traits
                }
            }
        };
    }

    public void HandleFile( string rootDir, ClassInfo classInfo ) {

    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string combinedPath = Path.Combine( rootDir, outProps.finalize_path );
        StringBuilder sb = new( );

        string preamble = mCfg.ResolveFilePreamble( combinedPath );
        sb.Append( preamble );

        string baseFileBlueprint = mCfg.GetBlueprint( "editor_traits_basefile", "\n" );

        string result = baseFileBlueprint;



    }

    private string inject_category_icons_traits( EditorTraitConfig cfg ) {
        string blueprint = mCfg.GetBlueprint( cfg.mBlueprintName, "\n" );

        int index = blueprint.FindTokenIndex( cfg.mBlueprintName, cfg.mToken );

        string preTraits = blueprint.Substring(cfg.)
        
        return ""; 
    }   

}
