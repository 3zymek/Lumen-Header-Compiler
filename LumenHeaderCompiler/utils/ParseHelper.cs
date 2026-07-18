using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;

internal class ParseHelper {

    private string? mFunctionNamespace = null;
    private readonly ConfigFile mCfg;

    public ParseHelper( ConfigFile cfg ) {

        mCfg = cfg;

        string fnNamespace = mCfg.GetTemplate( "parsing_namespace" ).Trim( );
        if (fnNamespace != "")
            mFunctionNamespace = fnNamespace;

    }

    public string BuildParseFunction( ClassInfo info, string sourceFile ) {

        string varName = mCfg.GetTemplate( "parse_fn_var" );
        string parseFn = mCfg.GetBlueprint( "parse_fn", "\n" ).FormatWith( new( ) {
            { "ClassName", info.mTypeName },
            { "Var", varName }
        } );
        
        int index = parseFn.FindTokenIndex( "parse_function", "{Fields}" );
        string alignment = parseFn.CalculateIndent( index );

        StringBuilder fieldsSb = new( );
        for (int i = 0; i < info.mFields.Count; i++) {

            FieldInfo field = info.mFields[i];
            string reader = mCfg.TypeToReader( field.mType ) ??
                throw new Exception( $"Unknown type: '{field.mType}' in {info.mTypeName}.{field.mName}" );

            string blueprint = i == 0 ? mCfg.GetBlueprint( "parse_fn_field_first", "\n" ) : mCfg.GetBlueprint( "parse_fn_field_next", "\n" );

            var formats = new Dictionary<string, string>( ) {
                { "FieldName", field.mName },
                { "Var", varName },
                { "Reader", reader },
            };
            string formattedBlock = blueprint.FormatWith( formats );

            if (i > 0)
                fieldsSb.Append( alignment );

            formattedBlock = formattedBlock.Replace( "\n", "\n" + alignment );
            fieldsSb.AppendLine( formattedBlock );

        }

        string result = parseFn.Replace( "{Fields}", fieldsSb.ToString( ) );

        return result;

    }

    public string MakeParseRegisteryDefinition( string finalizePath, List<ClassGeneratedInfo> classInfos, OutputProperties outProps ) {

        finalizePath.AssertFile( );

        var relativeIncludes = classInfos
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( finalizePath )!, absPath ) );

        string functionTemplate = mCfg.GetBlueprint( "parse_fn_register", "\n" );
        functionTemplate = functionTemplate.Replace( "{Signature}", mCfg.GetTemplate( "parse_fn_register_signature" ) );

        int index = functionTemplate.FindTokenIndex( "parse_fn_register", "{Fields}" );
        string alignment = functionTemplate.CalculateIndent( index );

        string fieldTemplate = mCfg.GetBlueprint( "parse_fn_register_field", "\n" );
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

        string result = functionTemplate.Replace( "{Fields}", sb.ToString( ) );

        if (mFunctionNamespace != null) {
            result = result.InjectToNamespace( mFunctionNamespace );
        }

        return result;

    }

    public string MakeParseRegisteryDeclaration( string finalizePath ) {

        finalizePath.AssertFile( );

        string signature = mCfg.GetTemplate( "parse_fn_register_signature" ) + ";";

        if (mFunctionNamespace != null)
            signature = signature.InjectToNamespace( mFunctionNamespace );
        
        return signature;

    }

}
