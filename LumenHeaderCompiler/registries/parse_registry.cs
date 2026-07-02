using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace lhc;

internal class ParseRegistry : IRegistry {

    private readonly ConfigFile mCfg;

    public ParseRegistry( ConfigFile cfg ) {
        mCfg = cfg;
    }

    public void GenerateParseFn( StringBuilder sb, ClassInfo info ) {

        string varName = mCfg.GetTemplate( "parse_fn_var" );
        string parseFn = string.Join( '\n', mCfg.GetFunctionTemplate( "parse_fn" ) ).FormatWith( new( ) {
            { "ClassName", info.mTypeName },
            { "Var", varName }
        } );

        int index = parseFn.IndexOf( "{Fields}" );

        if (index != -1) {

            string alignment = new( "" );
            for (int i = index - 1; i >= 0; i--) {

                char c = parseFn[i];

                if (c == '\n' || c == '\r')
                    break;

                if (char.IsWhiteSpace( c )) {
                    alignment = c + alignment;
                }
                else break;

            }


            string preFields = parseFn.Substring( 0, index );
            string postFields = parseFn.Substring( index + "{Fields}".Length );

            StringBuilder sbStatements = new( );
            for (int i = 0; i < info.mFields.Count; i++) {

                FieldInfo field = info.mFields[i];
                string reader = mCfg.TypeToReader( field.mType ) ??
                    throw new Exception( $"Unknown type: '{field.mType}' in {info.mTypeName}.{field.mName}" );

                List<string> template = i == 0 ? mCfg.GetFunctionTemplate( "parse_fn_field_first" ) : mCfg.GetFunctionTemplate( "parse_fn_field_next" );
                string mergedTemplate = string.Join( '\n', template );

                string formattedBlock = mergedTemplate.FormatWith( new( )
                    {
                        { "FieldName", field.mName },
                        { "Var", varName },
                        { "Reader", reader },
                    }
                );

                if (i > 0)
                    sbStatements.Append( alignment );

                formattedBlock = formattedBlock.Replace( "\n", "\n" + alignment );
                sbStatements.AppendLine( formattedBlock );

            }

            string result = preFields + sbStatements.ToString( ) + postFields;
            sb.AppendLine( result );

        }
        else throw new Exception( $"Couldn't find Fields parameter in parse_function" );

    }

    public void Finalize( string rootDir, Dictionary<string, ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string filePath = Path.Combine( rootDir, outProps.output_path );
        string baseInclude = outProps.base_include;
        string functionNamespace = outProps.function_namespace;

        if (!File.Exists( filePath )) throw new Exception( $"Parse generator finalization sequence file not found: {filePath}" );

        StringBuilder sb = new( );
        var relativeIncludes = classInfos.Values
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( filePath )!, absPath ) );

        LhcPipeline.GeneratePreamble( sb, null, new[] { baseInclude }.Concat( relativeIncludes ) );

        //string registerFn = LhcPipeline.

        string mapName = "map";
        sb.AppendLine( $"namespace {functionNamespace}" + " {\n" );
        sb.AppendLine( $"\tinline void {mCfg.GetTemplate( "parse_fn_registry" ).FormatWith( "Param", mapName )}" + " {" );

        foreach (var (key, val) in classInfos) {
            sb.AppendLine( $"\t\t{mapName}[ HashString(\"{ val.mInfo.ResolveParseName() }\") ] = {val.mParseFnName};" );
        }

        sb.AppendLine( "\t}\n" ); // function
        sb.AppendLine( "} " + $"// namespace {functionNamespace}\n" ); // namespace

        string outputPath = Path.Combine(
            Path.GetDirectoryName( baseInclude )!,
            Path.GetFileNameWithoutExtension( baseInclude ) + ".generated.hpp"
        );

        File.WriteAllText( outputPath, sb.ToString( ) );

    }

}
