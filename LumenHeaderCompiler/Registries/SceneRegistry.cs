using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class SceneRegistry : IRegistry {

    private readonly ConfigFile mCfg;

    public SceneRegistry(ConfigFile cfg) {
        mCfg = cfg;
    }

    public void HandleFile( string sourceFile, ClassInfo info ) {
        
    }
    public void Finalize( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        Blueprint functionBaseBp = mCfg.GetBlueprint( "scene_registry_fn", "\n" );

        int fieldsIndex = functionBaseBp.FindTokenIndex( "{Fields}" );
        string fieldsAlign = functionBaseBp.CalculateIndent( fieldsIndex );

        StringBuilder fieldsSb = new( );

        for (int i = 0; i < classInfos.Count; i++) {
            var info = classInfos[i];
            Blueprint fieldBp = mCfg.GetBlueprint( "scene_registry_fn_field", "\n" );

            var formats = new Dictionary<string, string>( ) {   
                { "ClassName", info.mInfo.mTypeName },
                { "DeserializeFn", info.mDeserializeFnName },
                { "SerializationName", info.mInfo.ResolveSerializationName() }
            };

            fieldBp.FormatWith( formats );

            string fieldContent = fieldBp.mContent.Replace( "\n", $"\n{fieldsAlign}" );

            if (i > 0) {
                fieldsSb.AppendLine( );
            }

            fieldsSb.Append( fieldContent );
        }

        string signature = mCfg.GetTemplate( "scene_registry_fn_signature" );

        functionBaseBp
            .Replace( "{Signature}", signature )
            .Replace( "{Fields}", fieldsSb.ToString( ) );

        Blueprint fileBaseBp = mCfg.GetBlueprint( "scene_registry_basefile", "\n" );
        int index = fileBaseBp.FindTokenIndex( "{SceneRegistryBody}" );
        string alignment = fileBaseBp.CalculateIndent( index );

        string functionContent = functionBaseBp.mContent.Replace( "\n", $"\n{alignment}" );

        fileBaseBp.Replace( "{SceneRegistryBody}", functionContent );

        var relativeIncludes = classInfos
            .Select( v => v.mOriginalFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( LhcPipeline.mRootDir, absPath ).Replace( '\\', '/' ) );

        string preamble = mCfg.ResolveFilePreamble( null, false, relativeIncludes );

        string result = preamble + "\n" + fileBaseBp.mContent;

        string outputPath = Path.Combine( LhcPipeline.mRootDir, outProps.finalize_path ).MakeGeneratedPath( "cpp" );
        outputPath.EnsureDirectory( );

        File.WriteAllText( outputPath, result );

    }



}

