using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal static class ParseHelper {

    public static string BuildParseFunctions( this ClassInfo info, string sourceFile, ConfigFile cfg ) {

        StringBuilder sb = new( );

        string varName = cfg.GetTemplate( "parse_fn_var" );
        string parseFn = string.Join( '\n', cfg.GetBlueprint( "parse_fn" ) ).FormatWith( new( ) {
            { "ClassName", info.mTypeName },
            { "Var", varName }
        } );

        int index = parseFn.IndexOf( "{Fields}" );

        if (index == -1) throw new ArgumentNullException( $"Couldn't find Fields parameter in parse_function" );

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
            string reader = cfg.TypeToReader( field.mType ) ??
                throw new Exception( $"Unknown type: '{field.mType}' in {info.mTypeName}.{field.mName}" );

            List<string> template = i == 0 ? cfg.GetBlueprint( "parse_fn_field_first" ) : cfg.GetBlueprint( "parse_fn_field_next" );
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

        return sb.ToString( );

    }

    public static string FinalizeParseRegistry( string rootDir, List<ClassGeneratedInfo> classInfos, OutputProperties outProps, ConfigFile cfg ) {

        string fullFilePath = Path.Combine( rootDir, outProps.finalize_path );

        if (!File.Exists( fullFilePath )) throw new Exception( $"{outProps.registry_type} output path file not found: {fullFilePath}" );

        var relativeIncludes = classInfos
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( fullFilePath )!, absPath ) );

        StringBuilder sb = new( );

        string preamble = cfg.ResolveFilePreamble( null, new[] { outProps.finalize_path }.Concat( relativeIncludes ) );
        sb.AppendLine( preamble );

        string parseRegisterFn = string.Join( '\n', cfg.GetBlueprint( "parse_fn_register" ) );
        int index = parseRegisterFn.IndexOf( "{Fields}" );

        if (index == -1) throw new Exception( $"Couldn't find Fields parameter in parse_register_function" );

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

        string registerField = string.Join( "\n", cfg.GetBlueprint( "parse_fn_register_field" ) );

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

        return sb.ToString( );

    }

}
