using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;


internal class ParseHelper {
    private readonly ConfigFile mCfg;

    public ParseHelper( ConfigFile cfg ) {

        mCfg = cfg;

    }

    public string BuildParseFunction( ClassInfo info ) {

        string functionSignature = mCfg.GetTemplate( "parse_fn_signature" );
        string functionBody = mCfg.GetBlueprint( "parse_fn_body", "\n" );
        string varName = mCfg.GetTemplate( "parse_fn_var" );

        var baseFormats = new Dictionary<string, string>( ) {
            { "ClassName", info.mTypeName },
            { "Var", varName }
        };

        string signaturedFunction = functionBody
            .FormatWith( "Signature", functionSignature )
            .FormatWith( baseFormats );

        int index = signaturedFunction.FindTokenIndex( "parse_fn_body", "{Fields}" );
        string alignment = signaturedFunction.CalculateIndent( index );

        StringBuilder fieldsSb = new( );
        for (int i = 0; i < info.mFields.Count; i++) {

            FieldInfo field = info.mFields[i];
            string reader = mCfg.TypeToReader( field.mTypeName ) ??
                throw new Exception( $"Unknown type: '{field.mTypeName}' in {info.mTypeName}.{field.mVariableName}" );
            
            string blueprint = i == 0
                ? mCfg.GetBlueprint( "parse_fn_field_first", "\n" )
                : mCfg.GetBlueprint( "parse_fn_field_next", "\n" );

            var fieldFormats = new Dictionary<string, string>( ) {
                { "FieldName", field.mVariableName },
                { "Var", varName },
                { "Reader", reader },
            };

            if (i != 0)
                fieldsSb.Append( alignment );

            string formattedFieldBlock = blueprint.FormatWith( fieldFormats );
            formattedFieldBlock = formattedFieldBlock.Replace( "\n", $"\n{alignment}" );
            fieldsSb.AppendLine( formattedFieldBlock );

        }

        string result = signaturedFunction.Replace( "{Fields}", fieldsSb.ToString( ) );

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

        /*
        if (mFunctionNamespace != null) {
            result = result.InjectToNamespace( mFunctionNamespace );
        }*/

        return result;

    }

    public string MakeParseRegisteryDeclaration( string finalizePath ) {

        finalizePath.AssertFile( );

        string signature = mCfg.GetTemplate( "parse_fn_register_signature" ) + ";";

        /*
        if (mFunctionNamespace != null)
            signature = signature.InjectToNamespace( mFunctionNamespace );
        */

        return signature;

    }

}
