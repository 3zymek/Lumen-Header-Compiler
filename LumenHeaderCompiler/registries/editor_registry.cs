using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace lhc;

internal class EditorRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private StringBuilder mSbuilder = new( );
    private StringBuilder mOutputBuilder = new( );
    private HashSet<string> mIncludes = new( );

    public EditorRegistry( ConfigFile cfg ) {
        mCfg = cfg;
    }

    public void GenerateFile( string sourceFile, ClassInfo info ) {

        string signature = mCfg.GetTemplate( "editor_fn_signature" ).FormatWith( "ClassName", info.mTypeName );
        mSbuilder.AppendLine( $"\tinline void {signature}" + " {\n" );
        string variableName = mCfg.GetTemplate( "editor_fn_comp_name" );
        string getter = mCfg.GetTemplate( "editor_fn_comp_getter" ).FormatWith( new Dictionary<string, string> {
            { "Var", variableName },
            { "ClassName", info.mTypeName }
        } );

        mSbuilder.AppendLine( $"\t\t{getter}" );

        string check = mCfg.GetTemplate( "editor_fn_getter_check" ).FormatWith( "Var", variableName );
        mSbuilder.AppendLine( $"\t\t{check}" );

        foreach (var field in info.mFields) {

            bool isDroppable = field.mArgs.mDroppable != null;

            string inspector;
            if (!isDroppable) {
                inspector = mCfg.TypeToInspector( field.mType ) ??
                    throw new Exception( $"Unknown type: '{field.mType}' in {info.mTypeName}.{field.mName}" );
            }
            else {
                inspector = mCfg.TypeToDroppableInspector( field.mType ) ?? 
                    throw new Exception( $"Unknown type: '{field.mType}' in {info.mTypeName}.{field.mName}" );
            }

            string fieldName = field.ResolveDisplayName(mCfg);
            var dict = new Dictionary<string, string> {
                { "DisplayName", fieldName },
                { "FieldName", field.mName },
                { "Var", variableName },
            };

            if (inspector.Contains( "{Speed}" ))
                dict["Speed"] = field.mArgs.mDragSpeed ?? mCfg.GetDefault( "drag_speed" );
            if (inspector.Contains( "{MinVal}" ))
                dict["MinVal"] = field.mArgs.mMinVal ?? mCfg.GetDefault( "min_val" );
            if (inspector.Contains( "{MaxVal}" ))
                dict["MaxVal"] = field.mArgs.mMaxVal ?? mCfg.GetDefault( "max_val" );
            if (inspector.Contains( "{DragSpeed}" ))
                dict["DragSpeed"] = field.mArgs.mDragSpeed ?? mCfg.GetDefault( "drag_speed" );
            if (inspector.Contains( "{Droppable}" ))
                dict["Droppable"] = field.mArgs.mDroppable ?? mCfg.GetDefault( "droppable" );

            mSbuilder.AppendLine( $"\t\t{inspector.FormatWith( dict )};" );
        }

        mSbuilder.AppendLine( "\n\t}\n" );
        //mIncludes.Add( info.mOriginalFilepath );
    }

    public void Finalize( string root, List<ClassGeneratedInfo> components, OutputProperties outProps ) {

        string editorDepMgrPath = new( "" ); //Path.Combine( root, mCfg.GetPath( "editor_dep_manager_path" ) );
        string editorDepMgrInclude = new( "" ); //mCfg.GetPath( "editor_dep_manager_include" );

        string editorFnNamespace = mCfg.GetTemplate( "editor_fn_namespace" );
        if (!File.Exists( editorDepMgrPath )) throw new Exception( $"Path to editor dependency manager is invalid: {editorDepMgrPath}" );

        /*
        var relativeIncludes = components.Values
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( editorDepMgrPath )!, absPath ) );

        //LhcPipeline.GeneratePreamble( mOutputBuilder, null, new[] { editorDepMgrInclude }.Concat( relativeIncludes ) );

        mOutputBuilder.AppendLine( $"namespace {editorFnNamespace}" + " {\n" );

        mOutputBuilder.Append( mSbuilder.ToString( ) );
        generate_editor_registry( mOutputBuilder, components );

        mOutputBuilder.AppendLine( "} " + $"// namespace {editorFnNamespace}\n" ); // namespace

        string outputPath = Path.Combine(
            Path.GetDirectoryName( editorDepMgrPath )!,
            Path.GetFileNameWithoutExtension( editorDepMgrPath ) + ".generated.hpp"
        );

        generate_category_color_getter( mOutputBuilder );
        generate_category_icon_getter( mOutputBuilder );
       
        File.WriteAllText( outputPath, mOutputBuilder.ToString( ) );
       */

    }

    private void generate_editor_registry( StringBuilder sb, Dictionary<string, ClassGeneratedInfo> components ) {

        string mapName = "map";
        sb.AppendLine( $"\tinline void {mCfg.GetTemplate( "editor_fn_registry" ).FormatWith( "Param", mapName )}" + " {" );

        foreach (var (key, val) in components) {
            string displayName = val.mInfo.ResolveDisplayName();
            string category = val.mInfo.mArgs.mCategoryName ?? mCfg.GetDefault( "category" );
            sb.AppendLine( 
                $"\t\t{mapName}[ HashString( \"{val.mInfo.ResolveParseName()}\" ) ] = {{\n " +
                $"\t\t\t{val.mEditorFnName},\n " +
                $"\t\t\t{mCfg.GetTemplate("editor_fn_registry_add_fn").FormatWith("ClassName", val.mInfo.mTypeName)},\n" +
                $"\t\t\t{mCfg.GetTemplate("editor_fn_registry_remove_fn").FormatWith("ClassName", val.mInfo.mTypeName)},\n" +
                $"\t\t\t\"{displayName}\",\n" +
                $"\t\t\t\"{category}\",\n" +
                $"\t\t\t{mCfg.GetTemplate("editor_fn_registry_typeid").FormatWith("ClassName", val.mInfo.mTypeName)},\n" +
                $"\t\t}};" 
                );
        }

        sb.AppendLine( "\t}\n" ); // function

    }

    private string hex_to_vec4( string hex ) {
        hex = hex.TrimStart( '#' );
        float r = Convert.ToInt32( hex[0..2], 16 ) / 255.0f;
        float g = Convert.ToInt32( hex[2..4], 16 ) / 255.0f;
        float b = Convert.ToInt32( hex[4..6], 16 ) / 255.0f;
        return $"{r.ToString( "F2", CultureInfo.InvariantCulture )}f, {g.ToString( "F2", CultureInfo.InvariantCulture )}f, {b.ToString( "F2", CultureInfo.InvariantCulture )}f, 1.0f";
    }

    private void generate_category_color_getter( StringBuilder sb ) {

        string namespaceName = mCfg.GetTemplate( "get_category_color_namespace" );
        sb.AppendLine( $"namespace {namespaceName} {{\n" );

        string variableName = "category";
        string returnType = mCfg.GetTemplate( "get_category_color_return" );
        string signature = mCfg.GetTemplate( "get_category_color_signature" ).FormatWith( "VariableName", variableName );
        sb.AppendLine( $"\tinline {returnType} {signature} {{" );
        sb.AppendLine( $"\t\tstatic std::unordered_map<HashedString, {returnType}> sColors = {{" );
        foreach (var color in mCfg.category_colors) {

            sb.AppendLine( $"\t\t\t{{ HashString( \"{color.Key}\" ), {{ {hex_to_vec4( color.Value )} }} }}," );

        }

        sb.AppendLine( "\t\t};" );
        sb.AppendLine( $"\t\tauto it = sColors.find( HashString({variableName}) );" );
        sb.AppendLine( $"\t\treturn it != sColors.end( ) ? it->second : {returnType}( 1, 1, 1, 1 );" );
        sb.AppendLine( "\t}" );
        sb.AppendLine( $"}} // namespace {namespaceName}" );

    }

    private void generate_category_icon_getter( StringBuilder sb ) {

        string namespaceName = mCfg.GetTemplate( "get_category_icon_namespace" );
        sb.AppendLine( $"namespace {namespaceName} {{\n" );

        string variableName = "category";
        string returnType = mCfg.GetTemplate( "get_category_icon_return" );
        string signature = mCfg.GetTemplate( "get_category_icon_signature" ).FormatWith( "VariableName", variableName );
        sb.AppendLine( $"\tinline {returnType} {signature} {{" );
        sb.AppendLine( $"\t\tstatic std::unordered_map<HashedString, {returnType}> sIcons = {{" );
        foreach( var icon in mCfg.category_icons) {

            sb.AppendLine( $"\t\t\t{{ HashString( \"{icon.Key}\" ), {{ {icon.Value} }} }}," );

        }

        sb.AppendLine( "\t\t};" );
        sb.AppendLine( $"\t\tauto it = sIcons.find( HashString({variableName}) );" );
        sb.AppendLine( $"\t\treturn it != sIcons.end( ) ? it->second : {returnType}( );" );
        sb.AppendLine( "\t}" );
        sb.AppendLine( $"}} // namespace {namespaceName}" );

    }

}
