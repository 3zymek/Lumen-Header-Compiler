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

        string bpName = "ecs_traits_instance_basefile";
        string fileBase = mCfg.GetBlueprint( bpName, "\n" );

        int bodyIndex = fileBase.FindTokenIndex( bpName, "{TraitsBody}" );
        string bodyAlign = fileBase.CalculateIndent( bodyIndex );

        int extensionsIndex = fileBase.FindTokenIndex( bpName, "{TraitsExtensions}" );
        string extensionsAlign = fileBase.CalculateIndent( extensionsIndex );

        foreach(var info in classInfos) {

            string generatedBody = resolve_traits_body( info );
            string generatedExtensions = resolve_traits_extensions( info );

            generatedBody = generatedBody.Replace( "\n", $"\n{bodyAlign}" );
            generatedExtensions = generatedExtensions.Replace( "\n", $"\n{extensionsAlign}" );

            string preamble = mCfg.ResolveFilePreamble( info.mOriginalFilepath, true );

            string result = fileBase;
            result = fileBase
                .Replace( "{TraitsBody}", generatedBody )
                .Replace( "{TraitsExtensions}", generatedExtensions );

            File.WriteAllText( info.mGeneratedFilepath, preamble + result );
            
        }


    }

    private string resolve_traits_body( ClassGeneratedInfo classInfo ) {

        string traitBodyBlueprint = mCfg.GetBlueprint( "ecs_trait_generated_body", "\n" );

        var formats = new Dictionary<string, string>( )
        {
            { "ParseName", classInfo.mInfo.ResolveParseName( ) },
            { "DisplayName", classInfo.mInfo.ResolveDisplayName( ) },
            { "CategoryName", classInfo.mInfo.ResolveCategoryName( mCfg ) },
            { "ClassName", classInfo.mInfo.mTypeName },
            { "ParseFn", classInfo.mParseFnName }
        };

        return traitBodyBlueprint.FormatWith( formats );

    }

    private string resolve_traits_extensions( ClassGeneratedInfo classInfo ) {

        string extensionsBaseBlueprint = mCfg.GetBlueprint( "ecs_traits_generated_extensions", "\n" );
        foreach(var cfg in mTraitToConfig) {
            extensionsBaseBlueprint = inject_trait_type( cfg, extensionsBaseBlueprint, classInfo );
        }
        return extensionsBaseBlueprint;

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
