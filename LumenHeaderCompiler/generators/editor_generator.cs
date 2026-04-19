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

            string fieldName = HeaderGenerator.GetFieldDisplayName( field );
            var dict = new Dictionary<string, string> {
                { "DisplayName", fieldName },
                { "FieldName", field.mName },
                { "Var", variableName },
            };

            if (inspector.Contains( "{Speed}" ))
                dict["Speed"] = field.mArgs.mDragSpeed ?? HeaderGenerator.GetDefault( "drag_speed" );
            if (inspector.Contains( "{MinVal}" ))
                dict["MinVal"] = field.mArgs.mMinVal ?? HeaderGenerator.GetDefault( "min_val" );
            if (inspector.Contains( "{MaxVal}" ))
                dict["MaxVal"] = field.mArgs.mMaxVal ?? HeaderGenerator.GetDefault( "max_val" );
            if (inspector.Contains( "{DragSpeed}" ))
                dict["DragSpeed"] = field.mArgs.mDragSpeed ?? HeaderGenerator.GetDefault( "drag_speed" );

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

        generate_category_color_getter( mOutputBuilder );

        File.WriteAllText( outputPath, mOutputBuilder.ToString( ) );

    }

    private static void generate_editor_registry( StringBuilder sb, Dictionary<string, ClassGeneratedInfo> components ) {

        string mapName = "map";
        sb.AppendLine( $"\tinline void {HeaderGenerator.GetTemplate( "editor_fn_registry" ).FormatWith( "Param", mapName )}" + " {" );

        foreach (var (key, val) in components) {
            string displayName = HeaderGenerator.GetClassDisplayName( val.mInfo );
            string category = val.mInfo.mArgs.mCategoryName ?? HeaderGenerator.GetDefault( "category" );
            sb.AppendLine( $"\t\t{mapName}[ HashStr( \"{HeaderGenerator.GetClassParseName( val.mInfo )}\" ) ] = {{ {val.mEditorFnName}, \"{displayName}\", \"{category}\" }};" );
        }

        sb.AppendLine( "\t}\n" ); // function

    }

    private static string hex_to_vec4( string hex ) {
        hex = hex.TrimStart( '#' );
        float r = Convert.ToInt32( hex[0..2], 16 ) / 255.0f;
        float g = Convert.ToInt32( hex[2..4], 16 ) / 255.0f;
        float b = Convert.ToInt32( hex[4..6], 16 ) / 255.0f;
        return $"{r.ToString( "F2", CultureInfo.InvariantCulture )}f, {g.ToString( "F2", CultureInfo.InvariantCulture )}f, {b.ToString( "F2", CultureInfo.InvariantCulture )}f, 1.0f";
    }

    private static void generate_category_color_getter( StringBuilder sb ) {

        string namespaceName = HeaderGenerator.GetTemplate( "get_category_color_namespace" );
        sb.AppendLine( $"namespace {namespaceName} {{\n" );

        string variableName = "category";
        string returnType = HeaderGenerator.GetTemplate( "get_category_color_return" );
        string signature = HeaderGenerator.GetTemplate( "get_category_color_signature" ).FormatWith( "VariableName", variableName );
        sb.AppendLine( $"\tinline {returnType} {signature} {{" );
        sb.AppendLine( $"\t\tstatic std::unordered_map<HashedStr, {returnType}> colors = {{" );
        foreach (var color in HeaderGenerator.GetCategoryColors( )) {

            sb.AppendLine( $"\t\t\t{{ HashStr( \"{color.Key}\" ), {{ {hex_to_vec4( color.Value )} }} }}," );

        }

        sb.AppendLine( "\t\t};" );
        sb.AppendLine( $"\t\tauto it = colors.find( HashStr({variableName}) );" );
        sb.AppendLine( $"\t\treturn it != colors.end( ) ? it->second : {returnType}( 1, 1, 1, 1 );" );
        sb.AppendLine( "\t}" );
        sb.AppendLine( $"}} // namespace {namespaceName}" );

    }

}
