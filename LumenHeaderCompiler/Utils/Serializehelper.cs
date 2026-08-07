using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace lhc;

internal class SerializeHelper {

    private readonly ConfigFile mCfg;

    public SerializeHelper( ConfigFile cfg ) {

        mCfg = cfg;

    }

    public string BuildSerializeFunction( ClassInfo info ) {

        string functionSignature = mCfg.GetTemplate( "serialize_fn_signature" );
        Blueprint functionBodyBp = mCfg.GetBlueprint( "serialize_fn_body", "\n" );
        string varName = mCfg.GetTemplate( "serialize_fn_var" );

        var baseFormats = new Dictionary<string, string>( ) {
            {"ClassName", info.mTypeName },
            { "Var", varName },
            { "SerializationName", info.ResolveSerializationName() }
        };

        functionBodyBp
            .FormatWith( "Signature", functionSignature )
            .FormatWith( baseFormats );

        int index = functionBodyBp.FindTokenIndex( "{Fields}" );
        string alignment = functionBodyBp.CalculateIndent( index );

        StringBuilder sb = new( );
        for(int i = 0; i < info.mFields.Count; i++) {

            var field = info.mFields[i];
            string writer = mCfg.TypeToWriter( field.mTypeName ) ??
                throw new Exception( $"Unknown type: '{field.mTypeName}' in {info.mTypeName}.{field.mVariableName}" );

            Blueprint fieldBp = mCfg.GetBlueprint( "deserialize_fn_field", "\n" );

            var fieldFormats = new Dictionary<string, string>( ) {
                { "FieldName", field.mVariableName },
                { "FieldSerializationName", field.ResolveSerializationName( mCfg ) },
                { "Var", varName },
                { "Writer", writer },
            };

            if (i != 0)
                sb.Append( alignment );

            fieldBp
                .FormatWith( fieldFormats )
                .Replace( "\n", $"\n{alignment}" );

            sb.AppendLine( fieldBp.mContent );

        }

        return functionBodyBp.Replace("{Fields}", sb.ToString() ).mContent;
    }

}
