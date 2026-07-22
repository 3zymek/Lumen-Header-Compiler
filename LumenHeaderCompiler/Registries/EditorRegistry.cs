using System.CodeDom.Compiler;
using System.Globalization;
using System.Text;

namespace lhc;

internal class EditorRegistry : IRegistry {
    private readonly ConfigFile mCfg;
    private readonly StringBuilder mHeaderFile = new( );
    private readonly StringBuilder mSourceFile = new( );
    private readonly List<ClassInfo> mClasses = new( );
    private readonly string? mFunctionNamespace;

    public EditorRegistry( ConfigFile cfg ) {
        mCfg = cfg;

        string fnNamespace = mCfg.GetTemplate( "editor_namespace" );
        if (!string.IsNullOrWhiteSpace( fnNamespace )) {
            mFunctionNamespace = fnNamespace;
        }

        initialize_files( );
    }

    public void HandleFile( string sourceFile, ClassInfo info ) {
        append_class_editor_function( info );
        track_processed_file( sourceFile, info );
    }

    public void Finalize( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {
        finalize_files( Path.Combine( LhcPipeline.mRootDir, outProps.finalize_path ), classInfos );
    }

    private void initialize_files( ) {
        string preamble = mCfg.ResolveFilePreamble( null );
        mHeaderFile.AppendLine( preamble );

        if (mFunctionNamespace != null) {
            mHeaderFile.AppendLine( $"namespace {mFunctionNamespace} {{" );
            mSourceFile.AppendLine( $"namespace {mFunctionNamespace} {{" );
        }
    }

    private void track_processed_file( string sourceFile, ClassInfo info ) {
        mClasses.Add( info );
    }

    private void append_class_editor_function( ClassInfo info ) {
        var functionFormats = new Dictionary<string, string>
        {
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
            const string indent = "\t";
            result = indent + result.Replace( "\n", $"\n{indent}" );
        }

        mSourceFile.AppendLine( result );
    }

    private string inject_inspector_fields( string blueprint, ClassInfo info ) {
        int index = blueprint.FindTokenIndex( "editor_fn", "{Inspector}" );
        string alignment = blueprint.CalculateIndent( index );

        StringBuilder inspectorSb = new( );
        for (int i = 0; i < info.mFields.Count; i++) {
            string inspector = build_field_inspector( info.mFields[i] );
            string formattedInspector = inspector.Replace( "\n", $"\n{alignment}" );

            if (i != 0) {
                inspectorSb.Append( alignment );
            }
            inspectorSb.Append( formattedInspector );
        }

        return blueprint.Replace( "{Inspector}", inspectorSb.ToString( ) );
    }

    private string build_field_inspector( FieldInfo field ) {
        string variableName = mCfg.GetTemplate( "editor_fn_var" );
        string? inspector = field.mArgs.mDroppable != null
            ? mCfg.mTypesCfg.TypeToDroppableInspector( field.mTypeName )
            : mCfg.mTypesCfg.TypeToInspector( field.mTypeName );

        if (inspector == null) {
            throw new Exception( $"Unknown type: '{field.mTypeName}' in {field.mVariableName}" );
        }

        var formats = new Dictionary<string, string>
        {
            { "DisplayName", field.ResolveDisplayName(mCfg) },
            { "FieldName", field.mVariableName },
            { "Var", variableName },
            { "Speed", field.mArgs.mDragSpeed ?? mCfg.GetDefault("drag_speed") },
            { "MinVal", field.mArgs.mMinVal ?? mCfg.GetDefault("min_val") },
            { "MaxVal", field.mArgs.mMaxVal ?? mCfg.GetDefault("max_val") },
            { "DragSpeed", field.mArgs.mDragSpeed ?? mCfg.GetDefault("drag_speed") },
            { "Droppable", field.mArgs.mDroppable ?? mCfg.GetDefault("droppable") }
        };

        return inspector.FormatWith( formats );
    }

    private string inject_register_fields( string blueprint, List<ClassInfo> classInfos ) {
        int index = blueprint.FindTokenIndex( "editor_fn_register", "{Fields}" );
        string alignment = blueprint.CalculateIndent( index );

        string registerFieldBlueprint = mCfg.GetBlueprint( "editor_fn_register_field", "\n" );
        StringBuilder sb = new( );

        for (int i = 0; i < classInfos.Count; i++) {
            ClassInfo info = classInfos[i];
            var formats = new Dictionary<string, string>
            {
                { "DeserializeName", info.ResolveDeserializeName() },
                { "ClassName", info.mTypeName },
                { "DisplayName", info.ResolveDisplayName() },
                { "CategoryName", info.ResolveCategoryName(mCfg) }
            };

            string formatted = registerFieldBlueprint.FormatWith( formats );
            formatted = formatted.Replace( "\n", $"\n{alignment}" );

            if (i != 0) {
                sb.Append( alignment );
            }
            sb.AppendLine( formatted );
        }

        return blueprint.Replace( "{Fields}", sb.ToString( ) );
    }


    private void finalize_files( string finalizePath, List<ClassGeneratedInfo> classInfos ) {
        string editorRegisterSignature = mCfg.GetTemplate( "editor_fn_register_signature" );
        string editorRegister = mCfg.GetBlueprint( "editor_fn_register", "\n" );
        editorRegister = editorRegister.FormatWith( "Signature", editorRegisterSignature );

        string indent = mFunctionNamespace != null ? "\t" : "";
        mHeaderFile.AppendLine( $"{indent}{editorRegisterSignature};" );

        string registerSource = inject_register_fields( editorRegister, mClasses );
        if (mFunctionNamespace != null) {
            registerSource = indent + registerSource.Replace( "\n", $"\n{indent}" );
        }
        mSourceFile.AppendLine( registerSource );

        close_namespaces( );
        write_output_to_disk( finalizePath, classInfos );
    }

    private void close_namespaces( ) {
        if (mFunctionNamespace == null) return;

        mHeaderFile.AppendLine( $"}} // namespace {mFunctionNamespace}" );
        mSourceFile.AppendLine( $"}} // namespace {mFunctionNamespace}" );
    }

    private void write_output_to_disk( string finalizePath, List<ClassGeneratedInfo> classInfos ) {

        var relativeIncludes = classInfos
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( LhcPipeline.mRootDir, absPath ).Replace( '\\', '/' ) );

        string generatedHeaderPath = finalizePath.MakeGeneratedPath( "hpp" );
        string preamble = mCfg.ResolveFilePreamble( generatedHeaderPath, false, relativeIncludes );
        string sourceResult = $"{preamble}\n{mSourceFile}";

        string generatedSourcePath = finalizePath.MakeGeneratedPath( "cpp" );
        File.WriteAllText( generatedSourcePath, sourceResult );
        File.WriteAllText( generatedHeaderPath, mHeaderFile.ToString( ) );
    }
}