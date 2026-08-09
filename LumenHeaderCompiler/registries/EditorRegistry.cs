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

        Blueprint functionBp = mCfg.GetBlueprint( "editor_fn", "\n" );
        functionBp
            .FormatWith( "Signature", fnSignature )
            .FormatWith( functionFormats );

        Blueprint result = inject_inspector_fields( functionBp, info );

        if (mFunctionNamespace != null) {
            const string indent = "\t";
            result.mContent = indent + result.Replace( "\n", $"\n{indent}" ).mContent;
        }

        mSourceFile.AppendLine( result.mContent );
    }

    private Blueprint inject_inspector_fields( Blueprint bp, ClassInfo info ) {
        int index = bp.FindTokenIndex( "{Inspector}" );
        string alignment = bp.CalculateIndent( index );

        StringBuilder inspectorSb = new( );
        for (int i = 0; i < info.mFields.Count; i++) {
            string inspector = build_field_inspector( info.mFields[i] );
            string formattedInspector = inspector.Replace( "\n", $"\n{alignment}" );

            if (i != 0) {
                inspectorSb.Append( alignment );
            }
            inspectorSb.Append( formattedInspector );
        }

        return bp.Replace( "{Inspector}", inspectorSb.ToString( ) );
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

    private Blueprint inject_register_fields( Blueprint bp, List<ClassInfo> classInfos ) {

        int index = bp.FindTokenIndex( "{Fields}" );
        string alignment = bp.CalculateIndent( index );

        StringBuilder sb = new( );

        for (int i = 0; i < classInfos.Count; i++) {

            ClassInfo info = classInfos[i];

            string editorFnName = mCfg
                .GetTemplate( "editor_fn_name" )
                .FormatWith( "ClassName", info.mTypeName )
                .ResolveFunctionName( mCfg, mCfg.GetTemplate( "editor_namespace" ) );

            var formats = new Dictionary<string, string>
            {
                { "DeserializeName", info.ResolveSerializationName() },
                { "DrawInspectorFn", editorFnName },
                { "ClassName", info.mTypeName },
                { "DisplayName", info.ResolveDisplayName() },
                { "CategoryName", info.ResolveCategoryName(mCfg) }
            };

            Blueprint registerFieldBp = mCfg.GetBlueprint( "editor_registry_field", "\n" );

            registerFieldBp
                .FormatWith( formats )
                .Replace( "\n", $"\n{alignment}" );

            if (i != 0) {
                sb.Append( alignment );
            }
            sb.AppendLine( registerFieldBp.mContent );

        }

        return bp.Replace( "{Fields}", sb.ToString( ) );
    }


    private void finalize_files( string finalizePath, List<ClassGeneratedInfo> classInfos ) {

        string editorRegisterSignature = mCfg.GetTemplate( "editor_registry_fn_signature" );
        Blueprint editorRegisterBp = mCfg.GetBlueprint( "editor_registry_fn", "\n" );
        editorRegisterBp = editorRegisterBp.FormatWith( "Signature", editorRegisterSignature );

        string indent = mFunctionNamespace != null ? "\t" : "";
        mHeaderFile.AppendLine( $"{indent}{editorRegisterSignature};" );

        Blueprint registerSource = inject_register_fields( editorRegisterBp, mClasses );
        if (mFunctionNamespace != null) {
            registerSource.mContent = indent + registerSource.Replace( "\n", $"\n{indent}" ).mContent;
        }
        mSourceFile.AppendLine( registerSource.mContent );

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
            .Select( v => v.mOriginalFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( LhcPipeline.mRootDir, absPath ).Replace( '\\', '/' ) );

        string generatedHeaderPath = finalizePath.MakeGeneratedPath( "hpp" );

        Blueprint baseBp = mCfg.GetBlueprint( "editor_registry_basefile", "\n" );
        baseBp.FormatWith( "EditorRegistryBody", mSourceFile.ToString( ) );

        string preamble = mCfg.ResolveFilePreamble( null, false, relativeIncludes );
        string sourceResult = $"{preamble}\n{baseBp.mContent}";

        string generatedSourcePath = finalizePath.MakeGeneratedPath( "cpp" );
        File.WriteAllText( generatedSourcePath, sourceResult );
        File.WriteAllText( generatedHeaderPath, mHeaderFile.ToString( ) );
    }
}