using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace lhc;

internal enum EEcsTraitType {
    ParseName,
    DisplayName,
    CategoryName
}

internal class EcsTraitConfig {
    public string mToken { get; set; } = "";
    public string mBlueprintName { get; set; } = "";
}

internal class EcsTraitsRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private readonly Dictionary<EEcsTraitType, EcsTraitConfig> mTraitToConfig;
    public EcsTraitsRegistry( ConfigFile cfg ) {
        mCfg = cfg;
        mTraitToConfig = new( )
            {
                {
                EEcsTraitType.ParseName, new EcsTraitConfig
                {
                    mToken = "{ParseNameTraits}",
                    mBlueprintName = "ecs_trait_parse_name"
                }
            },
            {
                EEcsTraitType.DisplayName, new EcsTraitConfig
                {
                    mToken = "{DisplayNameTraits}",
                    mBlueprintName = "ecs_trait_display_name"
                }
            },
            {
                EEcsTraitType.CategoryName, new EcsTraitConfig
                {
                    mToken = "{CategoryNameTraits}",
                    mBlueprintName = "ecs_trait_category_name"
                }
            }
        };
    }
    private EcsTraitConfig get_config(EEcsTraitType type) {
        if(mTraitToConfig.TryGetValue( type, out var val ))
            return val;
        throw new ArgumentException( $"Missing config for trait: {type}" );
    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string combinedPath = Path.Combine( rootDir, outProps.finalize_path );
        StringBuilder sb = new( );

        string preamble = mCfg.ResolveFilePreamble( combinedPath );
        sb.Append( preamble );

        string baseFileBlueprint = mCfg.GetBlueprint( "ecs_traits_basefile", "\n" );

        string result = baseFileBlueprint;
        foreach (var (traitType, config) in mTraitToConfig) {
            result = inject_trait_type( config, result, classInfos );
        }

        sb.Append( result );

        string generatedPath = combinedPath.MakeGeneratedPath( "hpp" );
        File.WriteAllText( generatedPath, sb.ToString() );

    }

    private string inject_trait_type( EcsTraitConfig cfg, string baseStr, List<ClassGeneratedInfo> classInfos ) {

        int index = baseStr.FindTokenIndex( "ecs_traits_basefile", cfg.mToken );

        string preTraits = baseStr.Substring( 0, index );
        string postTraits = baseStr.Substring( index + cfg.mToken.Length );

        string alignment = baseStr.CalculateIndent( index );

        string parseNameTrait = mCfg.GetBlueprint( cfg.mBlueprintName, "\n" );
        StringBuilder sb = new( );
        for (int i = 0; i < classInfos.Count; i++) {

            ClassGeneratedInfo info = classInfos[i];

            var formats = new Dictionary<string, string>( )
            {
                { "ClassName", info.mInfo.mTypeName },
                { "ParseName", info.mInfo.ResolveParseName() },
                { "DisplayName", info.mInfo.ResolveDisplayName() },
                { "CategoryName", info.mInfo.ResolveCategoryName(mCfg) }
            };
            string formatted = parseNameTrait.FormatWith( formats );
            formatted = formatted.Replace( "\n", $"\n{alignment}" );

            if (i != 0)
                sb.Append( alignment );
            sb.AppendLine( formatted );

        }

        string result = preTraits + sb.ToString( ) + postTraits;
        return result;

    }

}
