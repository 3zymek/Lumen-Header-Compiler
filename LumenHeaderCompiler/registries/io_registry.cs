using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class IoRegistry : IRegistry {

    private readonly ConfigFile mCfg;
    public IoRegistry( ConfigFile cfg ) {
        mCfg = cfg;
    }

    public void HandleFile( string sourceFile, ClassInfo info ) {

        StringBuilder sb = new( );
        string preamble = mCfg.ResolveFilePreamble( sourceFile );
        sb.AppendLine( preamble );

        string parseFunctions = info.BuildParseFunctions( sourceFile, mCfg );
        sb.AppendLine( parseFunctions );


        /*
         * string serializeFunctions = info.BuildSerializeFunctions( sourceFile, mCfg );
         * sb.AppendLine( serializeFunctions );
         */

        /*
            TODO: create ParseHelper and SerializeHelper and use IoRegistry as bridge between parsing and serializing
            one file for both functions AND ADD COMPONENT NAME GETTERS FROM LhcPipeline ~03.07.2026
        */

        string generatedPath = sourceFile.MakeGeneratedPath( "hpp" );
        File.WriteAllText( generatedPath, sb.ToString( ) );

    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string combinedPath = Path.Combine( rootDir, outProps.finalize_path );

        finalize_header_file( combinedPath, classInfos, outProps );

    }

    private void finalize_header_file( string finalizePath, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        finalizePath.AssertFile( );

        var relativeIncludes = classInfos
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( finalizePath )!, absPath ) );

        StringBuilder sb = new( );
        sb.Append( mCfg.ResolveFilePreamble( finalizePath.MakeGeneratedPath( "hpp" ), relativeIncludes ) );
        sb.AppendLine( ParseHelper.FinalizeParseRegistry( finalizePath, classInfos, outProps, mCfg ) );

        string generatedPath = finalizePath.MakeGeneratedPath( "cpp" );
        File.WriteAllText( generatedPath, sb.ToString( ) );


    }

    private void finalize_source_file( string finalizePath, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        finalizePath.AssertFile( );

        StringBuilder sb = new( );
        sb.Append( mCfg.ResolveFilePreamble( finalizePath ) );
        sb.AppendLine( );

    }

}

