using System.Text;

namespace lhc;

internal record ClassGeneratedInfo(
    ClassInfo mInfo,
    string mGeneratedFilepath,
    string mOriginalFilepath,
    string mDeserializeFnName,
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

        mRegistries.Add( "deserialize_registry", new DeserializeRegistry( mCfg ) );
        mRegistries.Add( "serialize_registry", new SerializeRegistry( mCfg ) );
        mRegistries.Add( "editor_registry", new EditorRegistry( mCfg ) );
        mRegistries.Add( "ecs_traits_registry", new EcsTraitsRegistry( mCfg ) );
        mRegistries.Add( "editor_traits_registry", new EditorTraitsRegistry( mCfg ) );
        mRegistries.Add( "scene_registry", new SceneRegistry( mCfg ) );

    }

    public void HandleFile( string sourceFile, List<ClassInfo> classInfos ) {

        if (!File.Exists( sourceFile )) { throw new Exception( $"File {sourceFile} doesn't exist" ); }

        StringBuilder sb = new( );
        string generatedPath = sourceFile.MakeGeneratedPath( "hpp" );

        string preamble = mCfg.ResolveFilePreamble( sourceFile );
        sb.AppendLine( preamble );

        foreach (var info in classInfos) {

            string deserializeFnName = mCfg
                .GetTemplate( "deserialize_fn_name" )
                .FormatWith( "ClassName", info.mTypeName )
                .ResolveFunctionName( mCfg, mCfg.GetTemplate( "deserialize_namespace" ) );

            string serializeFnName = mCfg
                .GetTemplate( "serialize_fn_name" )
                .FormatWith( "ClassName", info.mTypeName )
                .ResolveFunctionName( mCfg, mCfg.GetTemplate( "serialize_namespace" ));

            string editorFnName = mCfg
                .GetTemplate( "editor_fn_name" )
                .FormatWith( "ClassName", info.mTypeName )
                .ResolveFunctionName( mCfg, mCfg.GetTemplate( "editor_namespace" ));

            mClassInfos.Add( new(
                mInfo: info,
                mGeneratedFilepath: generatedPath,
                mOriginalFilepath: sourceFile,
                mDeserializeFnName: deserializeFnName,
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
                registry.Finalize( mClassInfos, output );
            }
            else throw new Exception( $"Unsupported registry type: '{output.registry_type}'" );

        }

    }


}
