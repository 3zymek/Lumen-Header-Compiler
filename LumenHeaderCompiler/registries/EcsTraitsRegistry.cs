using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace lhc;

internal class EcsTraitConfig {
    public string mToken { get; set; } = "";
    public string mBlueprintName { get; set; } = "";
}

internal class EcsTraitsRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private readonly List<EcsTraitConfig> mTraitToConfig;
    public EcsTraitsRegistry( ConfigFile cfg ) {
        mCfg = cfg;
        mTraitToConfig = new( )
        {
            {
                new EcsTraitConfig
                {
                    mToken = "{ParseNameTrait}",
                    mBlueprintName = "ecs_trait_parse_name"
                }
            },
            {
                new EcsTraitConfig
                {
                    mToken = "{DisplayNameTrait}",
                    mBlueprintName = "ecs_trait_display_name"
                }
            },
            {
                new EcsTraitConfig
                {
                    mToken = "{CategoryNameTrait}",
                    mBlueprintName = "ecs_trait_category_name"
                }
            }
        };
    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string combinedPath = Path.Combine( rootDir, outProps.finalize_path );
        StringBuilder sb = new( );

        /*
        var extraIncludes = classInfos
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( combinedPath )!, absPath ) );
       */

        string preamble = mCfg.ResolveFilePreamble( null );
        sb.AppendLine( preamble );

        StringBuilder forwardDeclareSb = new( );
        foreach (var info in classInfos) {
            forwardDeclareSb.AppendLine( $"struct {info.mInfo.mTypeName};" );
        }

        string forwardDeclareNamespace = mCfg.GetTemplate( "ecs_traits_forward_declare_namespace" ).Trim( );
        string forwardDeclare = forwardDeclareSb.ToString( );
        if (forwardDeclareNamespace != "")
            forwardDeclare = forwardDeclare.InjectToNamespace( forwardDeclareNamespace );

        sb.AppendLine( forwardDeclare );

        string traitBaseBlueprint = mCfg.GetBlueprint( "ecs_trait_base", "\n" );

        StringBuilder traitsSb = new( );
        for( int i = 0; i < classInfos.Count; i++) {

            var info = classInfos[i];

            string formatted = traitBaseBlueprint.FormatWith( "ClassName", info.mInfo.mTypeName );

            string resolved = resolve_base_trait( formatted, info );
            traitsSb.AppendLine( resolved );

        }

        string baseFileBlueprint = mCfg.GetBlueprint( "ecs_traits_basefile", "\n" );
        int index = baseFileBlueprint.FindTokenIndex( "ecs_traits_basefile", "{TraitsBase}" );
        string alignment = baseFileBlueprint.CalculateIndent( index );

        string traitsAligned = traitsSb.ToString( ).Replace( "\n", $"\n{alignment}" );

        baseFileBlueprint = baseFileBlueprint.Replace( "{TraitsBase}", traitsAligned );

        sb.Append( baseFileBlueprint );

        string generatedPath = combinedPath.MakeGeneratedPath( "hpp" );
        File.WriteAllText( generatedPath, sb.ToString( ) );

    }

    private string resolve_base_trait( string baseStr, ClassGeneratedInfo classInfo ) {

        string result = baseStr;
        foreach (var cfg in mTraitToConfig) {
            result = inject_trait_type( cfg, result, classInfo );
        }

        return result;

    }

    private string inject_trait_type( EcsTraitConfig cfg, string baseStr, ClassGeneratedInfo classInfo ) {

        int index = baseStr.FindTokenIndex( "ecs_traits_basefile", cfg.mToken );
        string alignment = baseStr.CalculateIndent( index );

        string traitBlueprint = mCfg.GetBlueprint( cfg.mBlueprintName, "\n" );
        var formats = new Dictionary<string, string>( )
            {
                { "ClassName", classInfo.mInfo.mTypeName },
                { "ParseName", classInfo.mInfo.ResolveParseName() },
                { "DisplayName", classInfo.mInfo.ResolveDisplayName() },
                { "CategoryName", classInfo.mInfo.ResolveCategoryName(mCfg) }
            };

        string formatted = traitBlueprint.FormatWith( formats );

        string result = baseStr.Replace( cfg.mToken, formatted );
        return result;

    }

}
