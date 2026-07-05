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
    private readonly string mRootDir;

    private List<ClassGeneratedInfo> mClassInfos = new( );
    private readonly Dictionary<string, IRegistry> mRegistries = new( );

    public LhcPipeline( string rootDir, ConfigFile cfg ) {

        mCfg = cfg;
        mRootDir = rootDir;

        mRegistries.Add( "io_registry", new IoRegistry( mCfg ) );
        //mRegistries.Add( "editor_registry", new EditorRegistry( mCfg ) );
        /*
         {
            "registry_type": "editor_registry",
            "base_filepath": "mgrs/editor_dep_manager.hpp",
            "template_function": "editor_function",
            "function_namespace": "lum::editor"
         }
        */

    }

    public void GenerateFile( string sourceFile, List<ClassInfo> classInfos ) {

        if (!File.Exists( sourceFile )) { throw new Exception( $"File {sourceFile} doesn't exist" ); }

        StringBuilder sb = new( );
        string generatedPath = Path.Combine(
            Path.GetDirectoryName( sourceFile )!,
            Path.GetFileNameWithoutExtension( sourceFile ) + ".generated.hpp"
        );

        string preamble = mCfg.ResolveFilePreamble( sourceFile );
        sb.AppendLine( preamble );

        //GeneratePreamble( sb, sourceFile, new[] { mCfg.GetPath( "scene_dep_manager_include" ) } );
        foreach (var info in classInfos) {

            string compName = info.ResolveParseName( );
            string parseFnName = mCfg.GetTemplate( "parse_fn_name" );
            string editorFnName = mCfg.GetTemplate( "editor_fn_name" );
            string serializeFnName = mCfg.GetTemplate( "serialize_fn_name" );

            mClassInfos.Add(new(
                mInfo: info,
                mGeneratedFilepath: generatedPath,
                mOriginalFilepath: sourceFile,
                mParseFnName: parseFnName.FormatWith( "ClassName", info.mTypeName ),
                mSerializeFnName: serializeFnName.FormatWith( "ClassName", info.mTypeName ),
                mEditorFnName: editorFnName.FormatWith( "ClassName", info.mTypeName )
            ) );
            
            foreach( var output in mCfg.outputs ) {
                
                if(mRegistries.TryGetValue( output.registry_type, out var registry )) {
                    registry.GenerateFile( sourceFile, info );
                }
                else throw new Exception( $"Unsupported registry type: '{output.registry_type}'" );

            }
            //EditorRegistry.GenerateEditorFn( generatedInfo );

            /*
            GenerateNameGetterArgs args = new( );
            args.mSignature = mCfg.GetTemplate( "get_parse_name_signature" );
            args.mNamespace = mCfg.GetTemplate( "get_parse_name_namespace" );
            args.mReturnType = mCfg.GetTemplate( "get_parse_name_return" );
            args.mReturnVal = info.ResolveParseName( );
            generate_name_getter_fn( sb, info, args );

            args.mSignature = mCfg.GetTemplate( "get_display_name_signature" );
            args.mNamespace = mCfg.GetTemplate( "get_display_name_namespace" );
            args.mReturnType = mCfg.GetTemplate( "get_display_name_return" );
            args.mReturnVal = info.ResolveDisplayName( );
            generate_name_getter_fn( sb, info, args );

            args.mSignature = mCfg.GetTemplate( "get_category_name_signature" );
            args.mNamespace = mCfg.GetTemplate( "get_category_name_namespace" );
            args.mReturnType = mCfg.GetTemplate( "get_category_name_return" );
            args.mReturnVal = info.mArgs.mCategoryName ?? mCfg.GetDefault( "category" );
            generate_name_getter_fn( sb, info, args );
           */
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

    private struct GenerateNameGetterArgs {

        public string mNamespace;
        public string mReturnType;
        public string mSignature;
        public string mReturnVal;

    }

    //private ClassGeneratedInfo register_class_metadata( ClassInfo info, string sourceFile, string generatedPath ) {



    //}

    private void generate_name_getter_fn( StringBuilder sb, ClassInfo info, GenerateNameGetterArgs args ) {

        sb.AppendLine( $"namespace {args.mNamespace}" + " {\n" );
        sb.AppendLine( "\ttemplate<>" );
        sb.AppendLine(
            "\tinline " +
            args.mReturnType +
            " " +
            args.mSignature.FormatWith( "ClassName", info.mTypeName ) +
            " {"
            );
        sb.AppendLine( $"\t\treturn \"{args.mReturnVal}\";" );
        sb.AppendLine( "\t}\n" );
        sb.AppendLine( "} " + $"// namespace {args.mNamespace}" );

    }

}
