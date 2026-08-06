using System;
using System.Collections.Generic;
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


        }



        return "";
    }

}
