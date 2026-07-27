using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal delegate string InjectTrait( EditorTraitConfig cfg, string baseStr, List<ClassGeneratedInfo> infos );
internal class EditorTraitConfig {

    public InjectTrait mInjectFn { get; set; } = ( cfg, baseStr, infos ) =>
    {
        Console.WriteLine( "mInjectFn in EditorTraitConfig has no function established" );
        return baseStr;
    };

    public string mToken { get; set; } = "";
    public string mBlueprintName { get; set; } = "";

}

internal class EditorTraitsRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private readonly List<EditorTraitConfig> mTraitToConfig;

    public EditorTraitsRegistry( ConfigFile cfg ) {
        mCfg = cfg;
        mTraitToConfig = new( )
        {
            {
                new EditorTraitConfig
                {
                    mToken = "{CategoryIconTraits}",
                    mBlueprintName = "editor_trait_category_icon",
                    mInjectFn = inject_category_icons_traits
                }
            },
            {
                new EditorTraitConfig
                {
                    mToken = "{CategoryColorTraits}",
                    mBlueprintName = "editor_trait_category_color",
                    mInjectFn = inject_category_color_traits
                }
            }
        };
    }

    public void HandleFile( string sourceFile, ClassInfo classInfo ) {

    }

    public void Finalize( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string combinedPath = Path.Combine( LhcPipeline.mRootDir, outProps.finalize_path );
        StringBuilder sb = new( );

        string preamble = mCfg.ResolveFilePreamble( null );
        sb.Append( preamble );
        sb.AppendLine( mCfg.GetBlueprint( "editor_traits_basefile", "\n" ).mContent );

        string result = sb.ToString( );
        foreach (var cfg in mTraitToConfig) {

            result = cfg.mInjectFn( cfg, result, classInfos );

        }

        string generatedPath = combinedPath.MakeGeneratedPath( "hpp" );
        File.WriteAllText( generatedPath, result );

    }

    private string inject_category_icons_traits( EditorTraitConfig cfg, string baseStr, List<ClassGeneratedInfo> classInfos ) {

        var icons = mCfg.category_icons;
        Blueprint functionBp = mCfg.GetBlueprint( cfg.mBlueprintName, "\n" );
        Blueprint fieldsBp = mCfg.GetBlueprint( "editor_trait_category_icon_field", "" );

        int fieldsIndex = functionBp.FindTokenIndex( "{Fields}" );
        string fieldsAlignment = functionBp.CalculateIndent( fieldsIndex );

        int baseStrIndex = baseStr.FindTokenIndex( cfg.mToken );
        string baseAlignment = baseStr.CalculateIndent( baseStrIndex );

        StringBuilder sb = new( );
        bool firstLoop = true;
        foreach (var (category, icon) in icons) {
            var formats = new Dictionary<string, string>( ) {
                { "CategoryName", category },
                { "Icon", icon }
            };

            if (firstLoop) {
                firstLoop = false;
            }
            else {
                sb.Append( fieldsAlignment );
            }
            sb.AppendLine( fieldsBp.FormatWith( formats ).mContent );
        }

        functionBp
            .Replace( "{Fields}", sb.ToString( ) )
            .Replace( "\n", $"\n{baseAlignment}" );

        return baseStr.Replace( cfg.mToken, functionBp.mContent );
    }

    private string inject_category_color_traits( EditorTraitConfig cfg, string baseStr, List<ClassGeneratedInfo> classInfos ) {

        var colors = mCfg.category_colors;
        Blueprint functionBp = mCfg.GetBlueprint( cfg.mBlueprintName, "\n" );
        Blueprint fieldsBp = mCfg.GetBlueprint( "editor_trait_category_color_field", "\n" );

        int fieldsIndex = functionBp.FindTokenIndex( "{Fields}" );
        string fieldsAlignment = functionBp.CalculateIndent( fieldsIndex );

        int baseStrIndex = baseStr.FindTokenIndex( cfg.mToken );
        string baseAlignment = baseStr.CalculateIndent( baseStrIndex );

        StringBuilder sb = new( );
        bool firstLoop = true;
        foreach (var (category, color) in colors) {

            var formats = new Dictionary<string, string>( )
            {
                { "CategoryName", category },
                { "Color", color.HexToVector4() }
            };

            if (firstLoop) {
                firstLoop = false;
            }
            else {
                sb.Append( fieldsAlignment );
            }
            sb.AppendLine( fieldsBp.FormatWith( formats ).mContent );

        }

        functionBp
            .Replace( "{Fields}", sb.ToString( ) )
            .Replace( "\n", $"\n{baseAlignment}" );

        return baseStr.Replace( cfg.mToken, functionBp.mContent );

    }

}
