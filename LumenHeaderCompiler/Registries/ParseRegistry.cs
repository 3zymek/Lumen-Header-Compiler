using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace lhc;

internal class ParseRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private readonly ParseHelper mParseHelper;
    private readonly string? mParsingNamespace = null;


    public ParseRegistry( ConfigFile cfg ) {
        mCfg = cfg;
        mParseHelper = new ParseHelper( mCfg );

        string parsingNamespace = mCfg.GetTemplate( "parsing_namespace" ).Trim();
        if (!string.IsNullOrWhiteSpace( parsingNamespace ))
            mParsingNamespace = parsingNamespace;

    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

        // BLANK

    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        handle_header_file( rootDir, classInfos, outProps );
        handle_source_file( rootDir, classInfos, outProps );

    }


    private void handle_header_file( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string functionSignature = mCfg.GetTemplate( "parse_fn_signature" );

        StringBuilder sb = new( );
        foreach(var info in classInfos) {

            sb.AppendLine( $"{functionSignature.FormatWith( "ClassName", info.mInfo.mTypeName )};" );

        }

        string result = sb.ToString( );

        if (mParsingNamespace != null)
            result = result.InjectToNamespace( mParsingNamespace );

        string finalHeaderPath = Path.Combine( rootDir, outProps.finalize_path ).MakeGeneratedPath( "hpp" );
        finalHeaderPath.EnsureDirectory( );

        string preamble = mCfg.ResolveFilePreamble( null );
        File.WriteAllText( finalHeaderPath, preamble + result );


    }
    private void handle_source_file( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        StringBuilder sb = new( );
        foreach (var info in classInfos) {

            sb.AppendLine( mParseHelper.BuildParseFunction( info.mInfo ) );
        }

        string rawFunctionsCode = sb.ToString( );

        if (mParsingNamespace != null) {
            rawFunctionsCode = rawFunctionsCode.InjectToNamespace( mParsingNamespace );
        }

        string fileBase = mCfg.GetBlueprint( "parse_registry_basefile", "\n" );
        int index = fileBase.FindTokenIndex( "parse_registry_basefile", "{ParseFunctions}" );
        string alignment = fileBase.CalculateIndent( index );

        string alignedContent = rawFunctionsCode.Replace( "\n", $"\n{alignment}" );
        string result = fileBase.Replace( "{ParseFunctions}", alignedContent );

        string finalSourcePath = Path.Combine( rootDir, outProps.finalize_path ).MakeGeneratedPath( "cpp" );
        finalSourcePath.EnsureDirectory( );

        string targetDirectory = Path.GetDirectoryName( finalSourcePath )!;

        var relativeIncludes = classInfos
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( targetDirectory, absPath ).Replace( '\\', '/' ) );

        string headerToInclude = outProps.finalize_path.MakeGeneratedPath( "hpp" );
        string preamble = mCfg.ResolveFilePreamble( headerToInclude, false, relativeIncludes );

        File.WriteAllText( finalSourcePath, preamble + result );

    }

}
