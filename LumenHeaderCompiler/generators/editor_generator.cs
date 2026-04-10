using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace lhc;

internal static class EditorGenerator {

    private static StringBuilder mSbuilder = new( );
    private static StringBuilder mOutputBuilder = new( );
    private static HashSet<string> mIncludes = new( );

    public static void GenerateEditorFn( ClassGeneratedInfo info ) {

        string signature = HeaderGenerator.GetTemplate( "editor_fn_signature" ).FormatWith( "ClassName", info.mInfo.mTypeName );
        mSbuilder.AppendLine( $"\tinline void {signature}" + " {\n" );
        string variableName = HeaderGenerator.GetTemplate( "editor_fn_comp_name" );
        string getter = HeaderGenerator.GetTemplate( "editor_fn_comp_getter" ).FormatWith( new Dictionary<string, string> {
            { "Var", variableName },
            { "ClassName", info.mInfo.mTypeName }
        } );

        mSbuilder.AppendLine( $"\t\t{getter}" );

        string check = HeaderGenerator.GetTemplate( "editor_fn_getter_check" ).FormatWith( "Var", variableName );
        mSbuilder.AppendLine( $"\t\t{check}" );

        foreach (var field in info.mInfo.mFields) {

            string inspector = HeaderGenerator.TypeToInspector( field.mType ) ??
               throw new Exception( $"Unknown type: '{field.mType}' in {info.mInfo.mTypeName}.{field.mName}" );

            string fieldName = HeaderGenerator.GetFieldName( field );
            string displayName = field.mArgs.mDisplayName ?? char.ToUpper( fieldName[0] ) + fieldName.Substring( 1 );
            var dict = new Dictionary<string, string> {
                { "DisplayName", displayName },
                { "FieldName", field.mName },
                { "Var", variableName }
            };
            if (inspector.Contains( "{Speed}" ))
                dict["Speed"] = field.mArgs.mDragSpeed ?? HeaderGenerator.GetDefault( "drag_speed" );

            if (inspector.Contains( "{MinVal}" ))
                dict["MinVal"] = field.mArgs.mMinVal ?? HeaderGenerator.GetDefault( "min_val" );

            if (inspector.Contains( "{MaxVal}" ))
                dict["MaxVal"] = field.mArgs.mMaxVal ?? HeaderGenerator.GetDefault( "max_val" );

            mSbuilder.AppendLine( $"\t\t{inspector.FormatWith( dict )};" );
        }

        mSbuilder.AppendLine( "\n\t}\n" );
        mIncludes.Add( info.mOriginalFilepath );
    }

    public static void Finalize( string root, Dictionary<string, ClassGeneratedInfo> components ) {

        string editorDepMgrPath = Path.Combine( root, HeaderGenerator.GetPath( "editor_dep_manager_path" ) );
        string editorDepMgrInclude = HeaderGenerator.GetPath( "editor_dep_manager_include" );

        string editorFnNamespace = HeaderGenerator.GetTemplate( "editor_fn_namespace" );
        if (!File.Exists( editorDepMgrPath )) throw new Exception( $"Path to editor dependency manager is invalid: {editorDepMgrPath}" );

        var relativeIncludes = components.Values
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( editorDepMgrPath )!, absPath ) );

        HeaderGenerator.GeneratePreamble( mOutputBuilder, null, new[] { editorDepMgrInclude }.Concat( relativeIncludes ) );

        mOutputBuilder.AppendLine( $"namespace {editorFnNamespace}" + " {\n" );

        mOutputBuilder.Append( mSbuilder.ToString( ) );
        generate_editor_registry( mOutputBuilder, components );

        mOutputBuilder.AppendLine( "} " + $"// namespace {editorFnNamespace}\n" ); // namespace

        string outputPath = Path.Combine(
            Path.GetDirectoryName( editorDepMgrPath )!,
            Path.GetFileNameWithoutExtension( editorDepMgrPath ) + ".generated.hpp"
        );

        File.WriteAllText( outputPath, mOutputBuilder.ToString( ) );

    }

    private static void generate_editor_registry( StringBuilder sb, Dictionary<string, ClassGeneratedInfo> components ) {

        string mapName = "map";
        sb.AppendLine( $"\tinline void {HeaderGenerator.GetTemplate( "editor_fn_registry" ).FormatWith( "Param", mapName )}" + " {" );

        foreach (var (key, val) in components) {
            sb.AppendLine( $"\t\t{mapName}[ HashStr( \"{HeaderGenerator.GetClassName( val.mInfo )}\" ) ] = {val.mEditorFnName};" );
        }

        sb.AppendLine( "\t}\n" ); // function

    }

}
