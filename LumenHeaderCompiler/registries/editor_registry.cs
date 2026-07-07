using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;

namespace lhc;

internal class EditorRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private StringBuilder mHeaderFile = new( );
    private StringBuilder mSourceFile = new( );
    private List<string> mExtraIncludes = new( );
    private List<ClassInfo> mClasses = new( );
    private string? mFunctionNamespace = null;


    public EditorRegistry( ConfigFile cfg ) {

        mCfg = cfg;

        string fnNamespace = mCfg.GetTemplate( "editor_fn_namespace" );
        if (fnNamespace.Trim( ) != "")
            mFunctionNamespace = fnNamespace;

        initialize_files( );

    }

    public void HandleFile( string sourceFile, ClassInfo info ) {
        var functionFormats = new Dictionary<string, string> {
            { "ClassName", info.mTypeName },
            { "Var", mCfg.GetTemplate("editor_fn_var") }
        };

        string fnSignature = mCfg.GetTemplate( "editor_fn_signature" ).FormatWith( functionFormats );
        string hppIndent = mFunctionNamespace != null ? "\t" : "";
        mHeaderFile.AppendLine( $"{hppIndent}{fnSignature};" );

        string editorFn = string.Join( "\n", mCfg.GetBlueprint( "editor_fn" ) );
        editorFn = editorFn.FormatWith( "Signature", fnSignature ).FormatWith( functionFormats );

        string result = inject_inspector_fields( editorFn, info );

        if (mFunctionNamespace != null) {
            string indent = "\t";
            result = indent + result.Replace( "\n", $"\n{indent}" );
        }

        mSourceFile.AppendLine( result );
        mExtraIncludes.Add( Path.GetRelativePath( LhcPipeline.mRootDir, sourceFile ) );
        mClasses.Add( info );
    }

    private string inject_inspector_fields( string blueprint, ClassInfo info ) {
        int index = blueprint.IndexOf( "{Inspector}" );
        if (index == -1) throw new ArgumentNullException( "Couldn't find {Inspector} token in blueprint." );

        string alignment = "";
        for (int i = index - 1; i >= 0 && blueprint[i] != '\n' && blueprint[i] != '\r'; i--) {
            char c = blueprint[i];
            if (char.IsWhiteSpace( c ))
                alignment = c + alignment;
            else
                break;
        }

        string preInspector = blueprint.Substring( 0, index );
        string postInspector = blueprint.Substring( index + "{Inspector}".Length );

        StringBuilder inspectorSb = new( );
        for (int i = 0; i < info.mFields.Count; i++) {
            string inspector = build_field_inspector( info.mFields[i] );
            string formattedInspector = inspector.Replace( "\n", "\n" + alignment );

            if (i != 0) inspectorSb.Append( alignment );
            inspectorSb.Append( formattedInspector );
        }

        return preInspector + inspectorSb.ToString( ) + postInspector;
    }

    private string build_field_inspector( FieldInfo field ) {

        string variableName = mCfg.GetTemplate( "editor_fn_var" );
        string? inspector = field.mArgs.mDroppable != null
            ? mCfg.TypeToDroppableInspector( field.mType )
            : mCfg.TypeToInspector( field.mType );

        if (inspector == null)
            throw new Exception( $"Unknown type: '{field.mType}' in {field.mName}" );

        var formats = new Dictionary<string, string> {
            { "DisplayName", field.ResolveDisplayName(mCfg) },
            { "FieldName", field.mName },
            { "Var", variableName },
            { "Speed", field.mArgs.mDragSpeed ?? mCfg.GetDefault("drag_speed") },
            { "MinVal", field.mArgs.mMinVal ?? mCfg.GetDefault("min_val") },
            { "MaxVal", field.mArgs.mMaxVal ?? mCfg.GetDefault("max_val") },
            { "DragSpeed", field.mArgs.mDragSpeed ?? mCfg.GetDefault("drag_speed") },
            { "Droppable", field.mArgs.mDroppable ?? mCfg.GetDefault("droppable") }
        };

        return inspector.FormatWith( formats );
    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

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

    private string inject_register_fields( string blueprint, List<ClassInfo> classInfos ) {
        int index = blueprint.IndexOf( "{Fields}" );
        if (index == -1) throw new ArgumentNullException( "Couldn't find Inspector parameter in editor_fn_register" );

        string preFields = blueprint.Substring( 0, index );
        string postFields = blueprint.Substring( index + "{Fields}".Length );

        string alignment = new( "" );
        for (int i = index - 1; i >= 0 && blueprint[i] != '\n' && blueprint[i] != '\r'; i--) {
            char c = blueprint[i];
            if (char.IsWhiteSpace( c ))
                alignment = c + alignment;
            else break;
        }

        string registerFieldBlueprint = string.Join( '\n', mCfg.GetBlueprint( "editor_fn_register_field" ) );
        StringBuilder sb = new( );
        for (int i = 0; i < classInfos.Count; i++) {

            ClassInfo info = classInfos[i];

            var formats = new Dictionary<string, string>( )
            {
                { "ParseName", info.ResolveParseName() },
                { "ClassName", info.mTypeName },
                { "DisplayName", info.ResolveDisplayName() },
                { "CategoryName", info.ResolveCategoryName( mCfg.defaults["category"] ) }
            };

            string formatted = registerFieldBlueprint.FormatWith( formats );
            formatted = formatted.Replace( "\n", $"\n{alignment}" );

            if (i != 0)
                sb.Append( alignment );

            sb.Append( formatted );

        }

        string result = preFields + sb.ToString( ) + postFields;
        return result;
    }

    private void finalize_files( string finalizePath ) {

        string editorRegisterSignature = mCfg.GetTemplate( "editor_fn_register_signature" );
        string editorRegister = string.Join( '\n', mCfg.GetBlueprint( "editor_fn_register" ) );
        editorRegister = editorRegister.FormatWith( "Signature", editorRegisterSignature );

        string indent = (mFunctionNamespace != null) ? "\t" : "";
        mHeaderFile.AppendLine( $"{indent}{editorRegisterSignature};" );

        string registerSource = inject_register_fields( editorRegister, mClasses );
        if (mFunctionNamespace != null)
            registerSource = indent + registerSource.Replace( "\n", $"\n{indent}");
        mSourceFile.AppendLine( registerSource );

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
