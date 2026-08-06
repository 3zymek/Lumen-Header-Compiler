using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace lhc;

internal class DeserializeRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private readonly DeserializeHelper mDeserializeHelper;
    private readonly string? mNamespace = null;

    public DeserializeRegistry( ConfigFile cfg ) {

        mCfg = cfg;
        mDeserializeHelper = new DeserializeHelper( mCfg );

        string parsingNamespace = mCfg.GetTemplate( "deserialize_namespace" ).Trim( );
        if (!string.IsNullOrWhiteSpace( parsingNamespace ))
            mNamespace = parsingNamespace;

    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

        // BLANK

    }

    public void Finalize( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        handle_header_file( classInfos, outProps );
        handle_source_file( classInfos, outProps );

    }


    private void handle_header_file( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string functionSignature = mCfg.GetTemplate( "deserialize_fn_signature" );

        StringBuilder sb = new( );
        for (int i = 0; i < classInfos.Count; i++) {
            ClassInfo info = classInfos[i].mInfo;
            sb.AppendLine( $"{functionSignature.FormatWith( "ClassName", info.mTypeName )};" );
        }

        string rawSignatures = sb.ToString( );

        Blueprint baseBp = mCfg.GetBlueprint( "deserialize_registry_header_basefile", "\n" );
        int index = baseBp.FindTokenIndex( "{DeserializeFunctions}" );
        string alignment = baseBp.CalculateIndent( index );

        string alignedContent = rawSignatures.Replace( "\n", $"\n{alignment}" );
        baseBp.Replace( "{DeserializeFunctions}", alignedContent );

        if (mNamespace != null) {
            baseBp.mContent = baseBp.mContent.InjectToNamespace( mNamespace );
        }

        string finalHeaderPath = Path.Combine( LhcPipeline.mRootDir, outProps.finalize_path ).MakeGeneratedPath( "hpp" );
        finalHeaderPath.EnsureDirectory( );

        string preamble = mCfg.ResolveFilePreamble( null );
        File.WriteAllText( finalHeaderPath, preamble + baseBp.mContent );

    }
    private void handle_source_file( List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        StringBuilder sb = new( );
        foreach (var info in classInfos) {

            sb.AppendLine( mDeserializeHelper.BuildDeserializeFunction( info.mInfo ) );
        }

        string rawFunctionsCode = sb.ToString( );

        if (mNamespace != null) {
            rawFunctionsCode = rawFunctionsCode.InjectToNamespace( mNamespace );
        }

        Blueprint baseBp = mCfg.GetBlueprint( "deserialize_registry_source_basefile", "\n" );
        int index = baseBp.FindTokenIndex( "{DeserializeFunctions}" );
        string alignment = baseBp.CalculateIndent( index );

        string alignedContent = rawFunctionsCode.Replace( "\n", $"\n{alignment}" );
        baseBp.Replace( "{DeserializeFunctions}", alignedContent );

        string finalSourcePath = Path.Combine( LhcPipeline.mRootDir, outProps.finalize_path ).MakeGeneratedPath( "cpp" );
        finalSourcePath.EnsureDirectory( );

        string targetDirectory = Path.GetDirectoryName( finalSourcePath )!;

        var relativeIncludes = classInfos
            .Select( v => v.mOriginalFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( LhcPipeline.mRootDir, absPath ).Replace( '\\', '/' ) );

        string headerToInclude = outProps.finalize_path.MakeGeneratedPath( "hpp" );
        string preamble = mCfg.ResolveFilePreamble( null, false, relativeIncludes );

        File.WriteAllText( finalSourcePath, preamble + baseBp.mContent );

    }

}
