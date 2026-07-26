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
                    mToken = "{SerializationNameTrait}",
                    mBlueprintName = "ecs_trait_serialize_name"
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

    public void Finalize( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string bpName = "ecs_traits_instance_basefile";
        string fileBase = mCfg.GetBlueprint( bpName, "\n" );

        int bodyIndex = fileBase.FindTokenIndex( bpName, "{TraitsBody}" );
        string bodyAlign = fileBase.CalculateIndent( bodyIndex );

        int extensionsIndex = fileBase.FindTokenIndex( bpName, "{TraitsExtensions}" );
        string extensionsAlign = fileBase.CalculateIndent( extensionsIndex );

        foreach (var info in classInfos) {

            var (genBodyMacro, genBodyDefines) = resolve_macro( mCfg.supported_macros.generated_body_macro, info );
            var (genExtMacro, genExtDefines) = resolve_macro( mCfg.supported_macros.class_extensions_macro, info );

            string genBody = format_as_macro(
                genBodyMacro, resolve_traits_body( info )
            );

            string genExtensions = format_as_macro(
                genExtMacro, resolve_traits_extensions( info )
            );

            genBody = genBody.Replace( "\n", $"\n{bodyAlign}" );
            genExtensions = genExtensions.Replace( "\n", $"\n{extensionsAlign}" );

            string preamble = mCfg.ResolveFilePreamble( null, true );

            string result = fileBase;
            result = fileBase
                .Replace( "{TraitsBody}", genBody )
                .Replace( "{TraitsExtensions}", genExtensions );

            StringBuilder sb = new( );
            sb.AppendLine( preamble );
            sb.AppendLine( genBodyDefines );
            sb.AppendLine( genExtDefines );
            sb.AppendLine( result );

            File.WriteAllText( info.mGeneratedFilepath, sb.ToString( ) );

        }


    }

    private (string formattedMacro, string defines) resolve_macro( string baseMacro, ClassGeneratedInfo classInfo ) {

        int index = baseMacro.IndexOf( '(' );
        string undefMacro = baseMacro;
        if (index != -1) {
            undefMacro = baseMacro.Substring( 0, index );
        }

        string formattedMacro = index != -1
            ? baseMacro.Insert( index, $"_{classInfo.mInfo.mTypeName}" )
            : $"{baseMacro}_{classInfo.mInfo.mTypeName}";

        StringBuilder sb = new( );
        sb.AppendLine( $"#undef {undefMacro}" );
        sb.AppendLine( $"#define {baseMacro} {formattedMacro}" );

        return (formattedMacro, sb.ToString( ));

    }

    private string resolve_traits_body( ClassGeneratedInfo classInfo ) {

        string traitBodyBlueprint = mCfg.GetBlueprint( "ecs_trait_generated_body", "\n" );

        var formats = new Dictionary<string, string>( )
        {
            { "SerializationName", classInfo.mInfo.ResolveSerializationName( ) },
            { "DisplayName", classInfo.mInfo.ResolveDisplayName( ) },
            { "CategoryName", classInfo.mInfo.ResolveCategoryName( mCfg ) },
            { "ClassName", classInfo.mInfo.mTypeName },
            { "DeserializeFn", classInfo.mDeserializeFnName }
        };

        return traitBodyBlueprint.FormatWith( formats );

    }

    private string resolve_traits_extensions( ClassGeneratedInfo classInfo ) {

        string extensionsBaseBlueprint = mCfg.GetBlueprint( "ecs_traits_generated_extensions", "\n" );
        extensionsBaseBlueprint = extensionsBaseBlueprint.FormatWith( "ClassName", classInfo.mInfo.mTypeName );
        foreach (var cfg in mTraitToConfig) {
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
                { "SerializationName", classInfo.mInfo.ResolveSerializationName() },
                { "DisplayName", classInfo.mInfo.ResolveDisplayName() },
                { "CategoryName", classInfo.mInfo.ResolveCategoryName(mCfg) }
            };

        string formatted = traitBlueprint.FormatWith( formats );

        string result = baseStr.Replace( cfg.mToken, formatted );
        return result;

    }

    private string format_as_macro( string macroName, string content ) {
        var lines = content.Split( new[] { "\r\n", "\n" }, StringSplitOptions.None );
        StringBuilder sb = new( );
        sb.AppendLine( $"#define {macroName} \\" );

        for (int i = 0; i < lines.Length; i++) {

            string lineContent = lines[i];
            if (i == lines.Length - 1) {
                sb.AppendLine( $"\t{lineContent}" );
            }
            else {
                sb.AppendLine( $"\t{lineContent} \\" );
            }
        }
        return sb.ToString( );
    }

}

