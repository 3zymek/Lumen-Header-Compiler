using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace lhc;

internal class EditorRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private StringBuilder mHeaderFile = new( );
    private StringBuilder mSourceFile = new( );
    private List<string> mExtraIncludes = new( );
    private string? mFunctionNamespace = null;


    public EditorRegistry( ConfigFile cfg ) {

        mCfg = cfg;

        string fnNamespace = mCfg.GetTemplate( "editor_fn_namespace" );
        if (fnNamespace.Trim( ) != "")
            mFunctionNamespace = fnNamespace;

        initialize_files( );

    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

        Dictionary<string, string> functionFormats = new( ) {
            { "ClassName", info.mTypeName },
            { "Var", mCfg.GetTemplate("editor_fn_var") }
        };

        string fnSignature = mCfg.GetTemplate( "editor_fn_signature" ).FormatWith( functionFormats );
        string indent = mFunctionNamespace != null ? "\t" : "";
        string variableName = mCfg.GetTemplate( "editor_fn_var" );
        string editorFn = string.Join( $"\n", mCfg.GetBlueprint( "editor_fn" ) );
        editorFn = editorFn.FormatWith( "Signature", fnSignature );
        editorFn = editorFn.FormatWith( functionFormats );

        int index = editorFn.IndexOf( "{Inspector}" );
        if (index == -1) throw new ArgumentNullException( "Couldn't find Inspector parameter in editor_function" );

        string blueprintSpacing = "";
        for (int i = index - 1; i >= 0; i--) {
            char c = editorFn[i];
            if (c == '\n' || c == '\r')
                break;

            if (char.IsWhiteSpace( c ))
                blueprintSpacing = c + blueprintSpacing;
            else
                break;
        }

        mHeaderFile.AppendLine( $"{blueprintSpacing}{fnSignature};" );

        string preInspector = editorFn.Substring( 0, index );
        string postInspector = editorFn.Substring( index + "{Inspector}".Length );

        StringBuilder inspectorSb = new( );
        for (int i = 0; i < info.mFields.Count; i++) {

            FieldInfo field = info.mFields[i];
            bool isDroppable = field.mArgs.mDroppable != null;

            string? inspector = null;
            if (!isDroppable)
                inspector = mCfg.TypeToInspector( field.mType );
            else
                inspector = mCfg.TypeToDroppableInspector( field.mType );

            if (inspector == null)
                throw new Exception( $"Unknown type: '{field.mType}' in {info.mTypeName}.{field.mName}" );

            string displayName = field.ResolveDisplayName( mCfg );
            var formats = new Dictionary<string, string> {
                { "DisplayName", displayName },
                { "FieldName", field.mName },
                { "Var", variableName }
            };

            if (inspector.Contains( "{Speed}" ))
                formats["Speed"] = field.mArgs.mDragSpeed ?? mCfg.GetDefault( "drag_speed" );
            if (inspector.Contains( "{MinVal}" ))
                formats["MinVal"] = field.mArgs.mMinVal ?? mCfg.GetDefault( "min_val" );
            if (inspector.Contains( "{MaxVal}" ))
                formats["MaxVal"] = field.mArgs.mMaxVal ?? mCfg.GetDefault( "max_val" );
            if (inspector.Contains( "{DragSpeed}" ))
                formats["DragSpeed"] = field.mArgs.mDragSpeed ?? mCfg.GetDefault( "drag_speed" );
            if (inspector.Contains( "{Droppable}" ))
                formats["Droppable"] = field.mArgs.mDroppable ?? mCfg.GetDefault( "droppable" );

            inspector = inspector.FormatWith( formats );

            string formattedInspector = inspector.Replace( "\n", "\n" + blueprintSpacing );
            if (i != 0) {
                inspectorSb.Append( blueprintSpacing );
            }
            inspectorSb.AppendLine( formattedInspector );
        }

        string result = preInspector + inspectorSb.ToString( ) + postInspector;

        if (mFunctionNamespace != null) {
            result = indent + result.Replace( "\n", $"\n{indent}" );
        }

        mSourceFile.AppendLine( result );
        mExtraIncludes.Add( Path.GetRelativePath( LhcPipeline.mRootDir, sourceFile ) );
    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {



        /*
         string editorDepMgrPath = new( "" ); //Path.Combine( root, mCfg.GetPath( "editor_dep_manager_path" ) );
         string editorDepMgrInclude = new( "" ); //mCfg.GetPath( "editor_dep_manager_include" );

         string editorFnNamespace = mCfg.GetTemplate( "editor_fn_namespace" );
         if (!File.Exists( editorDepMgrPath )) throw new Exception( $"Path to editor dependency manager is invalid: {editorDepMgrPath}" );

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

        string outputPath = new( "" );
        foreach (var entry in mCfg.outputs) {
            if (entry.registry_type == "editor_registry") {
                outputPath = entry.finalize_path;
                break;
            }
        }

        finalize_files( Path.Combine( LhcPipeline.mRootDir, outputPath ) );

    }

    private void initialize_files( ) {

        string preamble = mCfg.ResolveFilePreamble( null );
        mHeaderFile.AppendLine( preamble );

        if (mFunctionNamespace != null) {
            mHeaderFile.AppendLine( $"namespace {mFunctionNamespace} {{" );
            mSourceFile.AppendLine( $"namespace {mFunctionNamespace} {{" );
        }

    }

    private void finalize_files( string finalizePath ) {

        if (mFunctionNamespace != null) {
            mHeaderFile.AppendLine( $"}} // namespace {mFunctionNamespace}" );
            mSourceFile.AppendLine( $"}} // namespace {mFunctionNamespace}" );
        }

        string preamble = mCfg.ResolveFilePreamble( finalizePath, mExtraIncludes );
        string result = $"{preamble}\n{mSourceFile.ToString( )}";

        string generatedSourcePath = finalizePath.MakeGeneratedPath( "cpp" );
        File.WriteAllText( generatedSourcePath, result );

        string generatedHeaderPath = finalizePath.MakeGeneratedPath( "hpp" );
        File.WriteAllText( generatedHeaderPath, mHeaderFile.ToString( ) );

    }

    private void generate_editor_registry( StringBuilder sb, Dictionary<string, ClassGeneratedInfo> components ) {

        string mapName = "map";
        sb.AppendLine( $"\tinline void {mCfg.GetTemplate( "editor_fn_registry" ).FormatWith( "Param", mapName )}" + " {" );

        foreach (var (key, val) in components) {
            string displayName = val.mInfo.ResolveDisplayName( );
            string category = val.mInfo.mArgs.mCategoryName ?? mCfg.GetDefault( "category" );
            sb.AppendLine(
                $"\t\t{mapName}[ HashString( \"{val.mInfo.ResolveParseName( )}\" ) ] = {{\n " +
                $"\t\t\t{val.mEditorFnName},\n " +
                $"\t\t\t{mCfg.GetTemplate( "editor_fn_registry_add_fn" ).FormatWith( "ClassName", val.mInfo.mTypeName )},\n" +
                $"\t\t\t{mCfg.GetTemplate( "editor_fn_registry_remove_fn" ).FormatWith( "ClassName", val.mInfo.mTypeName )},\n" +
                $"\t\t\t\"{displayName}\",\n" +
                $"\t\t\t\"{category}\",\n" +
                $"\t\t\t{mCfg.GetTemplate( "editor_fn_registry_typeid" ).FormatWith( "ClassName", val.mInfo.mTypeName )},\n" +
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
        foreach (var icon in mCfg.category_icons) {

            sb.AppendLine( $"\t\t\t{{ HashString( \"{icon.Key}\" ), {{ {icon.Value} }} }}," );

        }

        sb.AppendLine( "\t\t};" );
        sb.AppendLine( $"\t\tauto it = sIcons.find( HashString({variableName}) );" );
        sb.AppendLine( $"\t\treturn it != sIcons.end( ) ? it->second : {returnType}( );" );
        sb.AppendLine( "\t}" );
        sb.AppendLine( $"}} // namespace {namespaceName}" );

    }

}
