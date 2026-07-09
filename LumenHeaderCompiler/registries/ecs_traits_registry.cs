using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        string result = inject_parse_traits( baseFileBlueprint, classInfos );

        string generatedPath = Path.Combine(rootDir, outProps.finalize_path).MakeGeneratedPath( "hpp" );
        File.WriteAllText( generatedPath, result );

    }

    private string inject_parse_traits( string blueprint, List<ClassGeneratedInfo> classInfos ) {
        int index = blueprint.FindTokenIndex( "ecs_traits_basefile", "{ParseNameTraits}" );

        string preTraits = blueprint.Substring( 0, index );
        string postTraits = blueprint.Substring( index + "{ParseNameTraits}".Length );

        string alignment = blueprint.CalculateIndent( index );

        string parseNameTrait = string.Join('\n', mCfg.GetBlueprint( "ecs_trait_parse_name" ));
        StringBuilder sb = new( );
        for (int i = 0; i < classInfos.Count; i++) {

            ClassGeneratedInfo info = classInfos[i];

            var formats = new Dictionary<string, string>( )
            {
                {"ClassName", info.mInfo.mTypeName },
                {"ParseName", info.mInfo.ResolveParseName() }
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
    private void inject_display_traits( ) {

    }
    private void inject_category_traits( ) {

    }

}
