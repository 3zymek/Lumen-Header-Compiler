using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal static class ParseHelper {

    public static string BuildParseFunctions( this ClassInfo info, string sourceFile, ConfigFile cfg ) {

        string varName = cfg.GetTemplate( "parse_fn_var" );
        string parseFn = cfg.GetBlueprint( "parse_fn", "\n" ).FormatWith( new( ) {
            { "ClassName", info.mTypeName },
            { "Var", varName }
        } );

        int index = parseFn.FindTokenIndex( "parse_function", "{Fields}" );
        string alignment = parseFn.CalculateIndent( index );

        StringBuilder sb = new( );
        for (int i = 0; i < info.mFields.Count; i++) {

            FieldInfo field = info.mFields[i];
            string reader = cfg.TypeToReader( field.mType ) ??
                throw new Exception( $"Unknown type: '{field.mType}' in {info.mTypeName}.{field.mName}" );

            string blueprint = i == 0 ? cfg.GetBlueprint( "parse_fn_field_first", "\n" ) : cfg.GetBlueprint( "parse_fn_field_next", "\n" );

            var formats = new Dictionary<string, string>( ) {
                { "FieldName", field.mName },
                { "Var", varName },
                { "Reader", reader },
            };
            string formattedBlock = blueprint.FormatWith( formats );

            if (i > 0)
                sb.Append( alignment );

            formattedBlock = formattedBlock.Replace( "\n", "\n" + alignment );
            sb.AppendLine( formattedBlock );

        }

        parseFn = parseFn.Replace( "{Fields}", sb.ToString( ) );

        return parseFn;

    }

    public static string FinalizeParseRegistry( string finalizePath, List<ClassGeneratedInfo> classInfos, OutputProperties outProps, ConfigFile cfg ) {

        finalizePath.AssertFile( );

        var relativeIncludes = classInfos
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( finalizePath )!, absPath ) );

        string functionTemplate = cfg.GetBlueprint( "parse_fn_register", "\n" );
        functionTemplate = functionTemplate.Replace( "{Signature}", cfg.GetTemplate( "parse_fn_register_signature" ) );

        int index = functionTemplate.FindTokenIndex( "parse_fn_register", "{Fields}" );
        string alignment = functionTemplate.CalculateIndent( index );

        string fieldTemplate = cfg.GetBlueprint( "parse_fn_register_field", "\n" );
        StringBuilder sb = new( );
        for (int i = 0; i < classInfos.Count; i++) {

            var info = classInfos[i];
            var formats = new Dictionary<string, string>( )
            {
                { "ParseName", info.mInfo.ResolveParseName() },
                { "ParseFunctionName", info.mParseFnName }
            };

            if (i != 0)
                sb.Append( alignment );
            sb.AppendLine( fieldTemplate.FormatWith( formats ) );

        }

        return functionTemplate.Replace( "{Fields}", sb.ToString( ) );

    }

}
