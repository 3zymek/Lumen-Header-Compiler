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

    public void GenerateFile( string sourceFile, ClassInfo info ) {

        StringBuilder sb = new( );
        string preamble = mCfg.ResolveFilePreamble( sourceFile );
        sb.AppendLine( preamble );

        string varName = mCfg.GetTemplate( "parse_fn_var" );
        string parseFn = string.Join( '\n', mCfg.GetBlueprint( "parse_fn" ) ).FormatWith( new( ) {
            { "ClassName", info.mTypeName },
            { "Var", varName }
        } );

        int index = parseFn.IndexOf( "{Fields}" );

        if (index == -1) throw new Exception( $"Couldn't find Fields parameter in parse_function" );

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

            List<string> template = i == 0 ? mCfg.GetBlueprint( "parse_fn_field_first" ) : mCfg.GetBlueprint( "parse_fn_field_next" );
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

        string generatedPath = Path.Combine(
            Path.GetDirectoryName( sourceFile )!,
            Path.GetFileNameWithoutExtension( sourceFile ) + ".generated.hpp" 
            );

        File.WriteAllText( generatedPath, sb.ToString( ) );

    }

    public void Finalize( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        string fullFilePath = Path.Combine( rootDir, outProps.base_filepath );
        string functionNamespace = outProps.function_namespace;

        if (!File.Exists( fullFilePath )) throw new Exception( $"{outProps.registry_type} output path file not found: {fullFilePath}" );

        StringBuilder sb = new( );
        var relativeIncludes = classInfos
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( fullFilePath )!, absPath ) );

        string preamble = mCfg.ResolveFilePreamble( null, new[] { outProps.base_filepath }.Concat( relativeIncludes ) );
        sb.AppendLine( preamble );

        string parseRegisterFn = string.Join( '\n', mCfg.GetBlueprint( "parse_fn_register" ) );
        int index = parseRegisterFn.IndexOf( "{Fields}" );

        if (index != -1) {

            string alignment = new( "" );
            for (int i = index - 1; i >= 0; i--) {

                char c = parseRegisterFn[i];

                if (c == '\n' || c == '\r')
                    break;

                if (char.IsWhiteSpace( c )) {
                    alignment = c + alignment;
                }
                else break;

            }

            string preFields = parseRegisterFn.Substring( 0, index );
            string postFields = parseRegisterFn.Substring( "{Fields}".Length + index );

            string registerField = mCfg.GetTemplate( "parse_fn_register_field" );

            StringBuilder registrySb = new( );
            for (int i = 0; i < classInfos.Count; i++) {

                ClassGeneratedInfo info = classInfos[i];

                string formattedField = registerField.FormatWith( new( )
                {
                    { "ParseName", info.mInfo.ResolveParseName() },
                    { "ParseFunctionName", info.mParseFnName }
                } );

                if (i > 0)
                    registrySb.Append( alignment );
                registrySb.AppendLine( formattedField );

            }

            sb.Append( preFields );
            sb.Append( registrySb.ToString( ) );
            sb.Append( postFields );

            string outputPath = Path.Combine(
                Path.GetDirectoryName( fullFilePath )!,
                Path.GetFileNameWithoutExtension( fullFilePath ) + ".generated.hpp"
                );

            File.WriteAllText( outputPath, sb.ToString( ) );


        }
        else throw new Exception( $"Couldn't find Fields parameter in parse_register_function" );

    }

}
