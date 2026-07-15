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

        string fnNamespace = mCfg.GetTemplate( "editor_namespace" );
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

        string editorFn = mCfg.GetBlueprint( "editor_fn", "\n" );
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
        int index = blueprint.FindTokenIndex( "editor_fn", "{Inspector}" );

        string alignment = blueprint.CalculateIndent( index );

        string preInspector = blueprint.Substring( 0, index );
        string postInspector = blueprint.Substring( index + "{Inspector}".Length );

        StringBuilder inspectorSb = new( );
        for (int i = 0; i < info.mFields.Count; i++) {
            string inspector = build_field_inspector( info.mFields[i] );
            string formattedInspector = inspector.Replace( "\n", $"\n{alignment}" );

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

        finalize_files( Path.Combine( rootDir, outProps.finalize_path ) );

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
        int index = blueprint.FindTokenIndex( "editor_fn_register", "{Fields}" );

        string alignment = blueprint.CalculateIndent( index );

        string registerFieldBlueprint = mCfg.GetBlueprint( "editor_fn_register_field", "\n" );
        StringBuilder sb = new( );
        for (int i = 0; i < classInfos.Count; i++) {

            ClassInfo info = classInfos[i];

            var formats = new Dictionary<string, string>( )
            {
                { "ParseName", info.ResolveParseName() },
                { "ClassName", info.mTypeName },
                { "DisplayName", info.ResolveDisplayName() },
                { "CategoryName", info.ResolveCategoryName( mCfg ) }
            };
            string formatted = registerFieldBlueprint.FormatWith( formats );
            formatted = formatted.Replace( "\n", $"\n{alignment}" );

            if (i != 0)
                sb.Append( alignment );
            sb.AppendLine( formatted );

        }

        string result = blueprint.Replace( "{Fields}", sb.ToString( ) );
        return result;
    }

    private void finalize_files( string finalizePath ) {

        string editorRegisterSignature = mCfg.GetTemplate( "editor_fn_register_signature" );
        string editorRegister = mCfg.GetBlueprint( "editor_fn_register", "\n" );
        editorRegister = editorRegister.FormatWith( "Signature", editorRegisterSignature );

        string indent = (mFunctionNamespace != null) ? "\t" : "";
        mHeaderFile.AppendLine( $"{indent}{editorRegisterSignature};" );

        string registerSource = inject_register_fields( editorRegister, mClasses );
        if (mFunctionNamespace != null)
            registerSource = indent + registerSource.Replace( "\n", $"\n{indent}" );
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

}
