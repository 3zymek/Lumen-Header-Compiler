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

        string bpName = "scene_registry_fn";
        string functionBase = mCfg.GetBlueprint( bpName, "\n" );

        int fieldsIndex = functionBase.FindTokenIndex( bpName, "{Fields}" );
        string fieldsAlign = functionBase.CalculateIndent( fieldsIndex );

        StringBuilder fieldsSb = new( );

        for (int i = 0; i < classInfos.Count; i++) {
            var info = classInfos[i];
            var fieldTemplateLines = mCfg.GetBlueprint( "scene_registry_fn_field", "\n" );

            var formats = new Dictionary<string, string>( )
            {
                { "ClassName", info.mInfo.mTypeName },
                { "DeserializeFn", info.mDeserializeFnName },
                { "SerializationName", info.mInfo.ResolveSerializationName() }
            };

            string formattedField = fieldTemplateLines.FormatWith( formats );
            formattedField = formattedField.Replace( "\n", $"\n{fieldsAlign}" );

            if (i > 0) {
                fieldsSb.AppendLine( );
                fieldsSb.Append( fieldsAlign );
            }

            fieldsSb.Append( formattedField );
        }

        string signature = mCfg.GetTemplate( "scene_registry_fn_signature" );

        string result = functionBase
            .Replace( "{Signature}", signature )
            .Replace( "{Fields}", fieldsSb.ToString( ) );

        var relativeIncludes = classInfos
            .Select( v => v.mOriginalFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( LhcPipeline.mRootDir, absPath ).Replace( '\\', '/' ) );

        string preamble = mCfg.ResolveFilePreamble( null, false, relativeIncludes );
        string fileBase = mCfg.GetBlueprint( "scene_registry_basefile", "\n" );
        int index = fileBase.FindTokenIndex( "scene_registry_basefile", "{SceneRegistryBody}" );
        string alignment = fileBase.CalculateIndent( index );

        result = result.Replace( "\n", $"\n{alignment}" );
        result = fileBase.Replace( "{SceneRegistryBody}", result );
        result = preamble + result;

        string outputPath = Path.Combine( LhcPipeline.mRootDir, outProps.finalize_path ).MakeGeneratedPath( "cpp" );

        outputPath.EnsureDirectory( );

        File.WriteAllText( outputPath, result );
    }

    

}

