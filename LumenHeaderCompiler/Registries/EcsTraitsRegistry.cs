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
                    mToken = "{Body}",
                    mBlueprintName = "ecs_trait_extensions_body"
                }
            }
        };
    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

    }

    public void Finalize( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        Blueprint baseBp = mCfg.GetBlueprint( "ecs_traits_instance_basefile", "\n" );

        int bodyIndex = baseBp.FindTokenIndex( "{TraitsBody}" );
        string bodyAlign = baseBp.CalculateIndent( bodyIndex );

        int extensionsIndex = baseBp.FindTokenIndex( "{TraitsExtensions}" );
        string extensionsAlign = baseBp.CalculateIndent( extensionsIndex );

        foreach (var info in classInfos) {

            var (genBodyMacro, genBodyDefines) = resolve_macro( mCfg.supported_macros.generated_body_macro, info );
            var (genExtMacro, genExtDefines) = resolve_macro( mCfg.supported_macros.class_extensions_macro, info );

            string genBody = format_as_macro(
                genBodyMacro, resolve_traits_body( info ).mContent
            );

            string genExtensions = format_as_macro(
                genExtMacro, resolve_traits_extensions( info ).mContent
            );

            genBody = genBody.Replace( "\n", $"\n{bodyAlign}" );
            genExtensions = genExtensions.Replace( "\n", $"\n{extensionsAlign}" );

            string preamble = mCfg.ResolveFilePreamble( null, true );

            Blueprint result = new( baseBp );
            result = result
                .Replace( "{TraitsBody}", genBody )
                .Replace( "{TraitsExtensions}", genExtensions );

            StringBuilder sb = new( );
            sb.AppendLine( preamble );
            sb.AppendLine( genBodyDefines );
            sb.AppendLine( genExtDefines );
            sb.AppendLine( result.mContent );

            File.WriteAllText( info.mGeneratedFilepath, sb.ToString( ) );

        }


    }

    private (string formattedMacro, string defines) resolve_macro( string baseMacro, ClassGeneratedInfo classInfo ) {

        
        string undefMacro = baseMacro.Split('(')[0];
        string formattedMacro = $"{undefMacro}_{classInfo.mInfo.mTypeName}";

        StringBuilder sb = new( );
        sb.AppendLine( $"#undef {undefMacro}" );
        sb.AppendLine( $"#define {baseMacro} {formattedMacro}" );

        return (formattedMacro, sb.ToString( ));

    }

    private Blueprint resolve_traits_body( ClassGeneratedInfo classInfo ) {

        Blueprint traitBodyBp = mCfg.GetBlueprint( "ecs_trait_generated_body", "\n" );

        string serializationName = classInfo.mInfo.ResolveSerializationName( );
        var formats = new Dictionary<string, string>( ) {
                { "ClassName", classInfo.mInfo.mTypeName },
                { "SerializationName", serializationName },
                { "DisplayName", classInfo.mInfo.ResolveDisplayName() },
                { "CategoryName", classInfo.mInfo.ResolveCategoryName(mCfg) },
                { "SerializationID", StringHasher.Hash(serializationName).ToString() }
            };

        return traitBodyBp.FormatWith( formats );

    }

    private Blueprint resolve_traits_extensions( ClassGeneratedInfo classInfo ) {

        Blueprint extensionsBaseBp = mCfg.GetBlueprint( "ecs_traits_generated_extensions", "\n" );
        extensionsBaseBp.FormatWith( "ClassName", classInfo.mInfo.mTypeName );

        foreach (var cfg in mTraitToConfig) {
            extensionsBaseBp = inject_trait_type( cfg, extensionsBaseBp, classInfo );
        }
        return extensionsBaseBp;

    }

    private Blueprint inject_trait_type( EcsTraitConfig cfg, Blueprint baseBp, ClassGeneratedInfo classInfo ) {

        int index = baseBp.FindTokenIndex( cfg.mToken );
        string alignment = baseBp.CalculateIndent( index );

        Blueprint traitBp = mCfg.GetBlueprint( cfg.mBlueprintName, "\n" );
        string serializationName = classInfo.mInfo.ResolveSerializationName( );
        var formats = new Dictionary<string, string>( ) {
            { "ClassName", classInfo.mInfo.mTypeName },
            { "SerializationName", serializationName },
            { "DisplayName", classInfo.mInfo.ResolveDisplayName() },
            { "CategoryName", classInfo.mInfo.ResolveCategoryName(mCfg) },
            { "SerializationID", StringHasher.Hash(serializationName).ToString() }
        };

        traitBp.FormatWith( formats )
               .Replace( "\n", $"\n{alignment}" );

        return baseBp.Replace( cfg.mToken, traitBp.mContent );
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

