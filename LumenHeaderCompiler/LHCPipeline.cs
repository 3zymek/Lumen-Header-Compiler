using System.Text;

namespace lhc;

internal record ClassGeneratedInfo(
    ClassInfo mInfo,
    string mGeneratedFilepath,
    string mOriginalFilepath,
    string mParseFnName,
    string mSerializeFnName,
    string mEditorFnName
    );

internal class LhcPipeline {

    private readonly ConfigFile mCfg;
    public static string mRootDir { get; private set; } = new( "" );

    private List<ClassGeneratedInfo> mClassInfos = new( );
    private readonly Dictionary<string, IRegistry> mRegistries = new( );

    public LhcPipeline( string rootDir, ConfigFile cfg ) {

        mCfg = cfg;
        mRootDir = rootDir;

        mRegistries.Add( "parse_registry", new ParseRegistry( mCfg ) );
        mRegistries.Add( "editor_registry", new EditorRegistry( mCfg ) );
        mRegistries.Add( "ecs_traits_registry", new EcsTraitsRegistry( mCfg ) );
        mRegistries.Add( "editor_traits_registry", new EditorTraitsRegistry( mCfg ) );

    }

    public void HandleFile( string sourceFile, List<ClassInfo> classInfos ) {

        if (!File.Exists( sourceFile )) { throw new Exception( $"File {sourceFile} doesn't exist" ); }

        StringBuilder sb = new( );
        string generatedPath = sourceFile.MakeGeneratedPath( "hpp" );

        string preamble = mCfg.ResolveFilePreamble( sourceFile );
        sb.AppendLine( preamble );

        foreach (var info in classInfos) {

            string compName = info.ResolveParseName( );

            string parseFnName = mCfg
                .GetTemplate( "parse_fn_name" )
                .FormatWith( "ClassName", info.mTypeName )
                .ResolveParseFunctionName( mCfg );

            string editorFnName = mCfg
                .GetTemplate( "editor_fn_name" )
                .FormatWith( "ClassName", info.mTypeName )
                .ResolveEditorFunctionName( mCfg );

            string serializeFnName = mCfg.GetTemplate( "serialize_fn_name" );

            mClassInfos.Add( new(
                mInfo: info,
                mGeneratedFilepath: generatedPath,
                mOriginalFilepath: sourceFile,
                mParseFnName: parseFnName,
                mSerializeFnName: serializeFnName,
                mEditorFnName: editorFnName
            ) );

            foreach (var output in mCfg.outputs) {

                if (mRegistries.TryGetValue( output.registry_type, out var registry )) {
                    registry.HandleFile( sourceFile, info );
                }
                else throw new Exception( $"Unsupported registry type: '{output.registry_type}'" );

            }

        }

    }

    public void Finalize( ) {

        StringBuilder sb = new( );

        foreach (var output in mCfg.outputs) {

            if (mRegistries.TryGetValue( output.registry_type, out var registry )) {
                registry.Finalize( mRootDir, mClassInfos, output );
            }
            else throw new Exception( $"Unsupported registry type: '{output.registry_type}'" );

        }

    }

}
