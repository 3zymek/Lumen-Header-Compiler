using System;
using System.Collections.Generic;
using System.Text;

namespace lhc;


internal class DeserializeHelper {
    private readonly ConfigFile mCfg;

    public DeserializeHelper( ConfigFile cfg ) {

        mCfg = cfg;

    }

    public string BuildDeserializeFunction( ClassInfo info ) {

        string functionSignature = mCfg.GetTemplate( "deserialize_fn_signature" );
        Blueprint functionBodyBp = mCfg.GetBlueprint( "deserialize_fn_body", "\n" );
        string varName = mCfg.GetTemplate( "deserialize_fn_var" );

        var baseFormats = new Dictionary<string, string>( ) {
            { "ClassName", info.mTypeName },
            { "Var", varName }
        };

        functionBodyBp
            .FormatWith( "Signature", functionSignature )
            .FormatWith( baseFormats );

        int index = functionBodyBp.FindTokenIndex( "{Fields}" );
        string alignment = functionBodyBp.CalculateIndent( index );

        StringBuilder fieldsSb = new( );
        for (int i = 0; i < info.mFields.Count; i++) {

            FieldInfo field = info.mFields[i];
            string reader = mCfg.TypeToReader( field.mTypeName ) ??
                throw new Exception( $"Unknown type: '{field.mTypeName}' in {info.mTypeName}.{field.mVariableName}" );

            Blueprint blueprint = i == 0
                ? mCfg.GetBlueprint( "deserialize_fn_field_first", "\n" )
                : mCfg.GetBlueprint( "deserialize_fn_field_next", "\n" );

            var fieldFormats = new Dictionary<string, string>( ) {
                { "FieldName", field.mVariableName },
                { "Var", varName },
                { "Reader", reader },
            };

            if (i != 0)
                fieldsSb.Append( alignment );

            blueprint
                .FormatWith( fieldFormats )
                .Replace( "\n", $"\n{alignment}" );

            fieldsSb.AppendLine( blueprint.mContent );

        }

        return functionBodyBp.Replace( "{Fields}", fieldsSb.ToString( ) ).mContent;

    }

}
