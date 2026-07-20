using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class IoRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    private ParseHelper mParseHelper;
    public IoRegistry( ConfigFile cfg ) {
        mCfg = cfg;
        mParseHelper = new ParseHelper( mCfg );
    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

        string blueprintName = "io_registry_instance_basefile";
        string baseTemplate = mCfg.GetBlueprint( blueprintName, "\n" );

        //string parseFn = mParseHelper.BuildParseFunction( info, sourceFile );
        int parseIndex = baseTemplate.FindTokenIndex( blueprintName, "{ParseFunction}" );
        string parseFnAlignment = baseTemplate.CalculateIndent( parseIndex );
        //parseFn = parseFn.Replace( "\n", $"\n{parseFnAlignment}" );
        //baseTemplate = baseTemplate.Replace( "{ParseFunction}", parseFn );

        StringBuilder sb = new( );
        string preamble = mCfg.ResolveFilePreamble( sourceFile );
        sb.AppendLine( preamble );
        sb.AppendLine( baseTemplate );

        /*
            TODO: create ParseHelper and SerializeHelper and use IoRegistry as bridge between parsing and serializing
            one file for both functions AND ADD COMPONENT NAME GETTERS FROM LhcPipeline ~03.07.2026
         */

        string generatedPath = sourceFile.MakeGeneratedPath( "hpp" );
        File.WriteAllText( generatedPath, sb.ToString( ) );

    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string combinedPath = Path.Combine( rootDir, outProps.finalize_path );

        finalize_header_file( combinedPath );
        finalize_source_file( combinedPath, classInfos, outProps );

    }

    private void finalize_source_file( string finalizePath, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        finalizePath.AssertFile( );

        var relativeIncludes = classInfos
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( finalizePath )!, absPath ) );

        StringBuilder sb = new( );
        sb.Append( mCfg.ResolveFilePreamble( finalizePath.MakeGeneratedPath( "hpp" ), false, relativeIncludes ) );
        sb.AppendLine( mParseHelper.MakeParseRegisteryDefinition( finalizePath, classInfos, outProps ) );

        string generatedPath = finalizePath.MakeGeneratedPath( "cpp" );
        File.WriteAllText( generatedPath, sb.ToString( ) );

    }

    private void finalize_header_file( string finalizePath ) {

        finalizePath.AssertFile( );

        StringBuilder sb = new( );
        sb.Append( mCfg.ResolveFilePreamble( finalizePath ) );
        sb.AppendLine( mParseHelper.MakeParseRegisteryDeclaration( finalizePath ) );

        string generatedPath = finalizePath.MakeGeneratedPath( "hpp" );
        File.WriteAllText( generatedPath, sb.ToString( ) );

    }

}

