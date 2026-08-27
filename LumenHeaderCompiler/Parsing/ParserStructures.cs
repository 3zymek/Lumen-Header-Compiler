using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace lhc;
internal class ParserArgument {
    public required string name { get; init; }
    public required string assignment_operator { get; init; }

    [JsonConverter( typeof( JsonStringEnumConverter ) )]
    public required TokenType expected_token { get; init; }

}

internal class ParserConfigFile {
    public required List<ParserArgument> property_args { get; init; }
    public required List<ParserArgument> class_args { get; init; }

    [JsonIgnore]
    public SupportedMacros mSupportedMacros { get; set; } = null!;

}

internal class QualifierArgs {

    public Dictionary<string, string> mProperties = new( );

    public string? mDisplayName => mProperties.GetValueOrDefault( "displayname" );
    public string? mParseName => mProperties.GetValueOrDefault( "parsename" );
    public string? mCategoryName => mProperties.GetValueOrDefault( "category" );
    public string? mMinVal => mProperties.GetValueOrDefault( "minval" );
    public string? mMaxVal => mProperties.GetValueOrDefault( "maxval" );
    public string? mDragSpeed => mProperties.GetValueOrDefault( "dragspeed" );
    public string? mDroppable => mProperties.GetValueOrDefault( "droppable" );

};
internal record FieldInfo( 
    string mTypeName, 
    QualifierArgs mArgs, 
    string mVariableName 
    );
internal record ClassInfo( 
    string mTypeName,
    QualifierArgs mArgs, 
    List<FieldInfo> mFields 
    );

internal delegate void ParseFn( );
internal class ParserMacroConfig {
    public ParseFn mParseFn { get; set; } = ( ) => {
        Console.WriteLine( "mParseFn in ParserMacroConfig has no function established" );
    };
    public required string mValue { get; init; }

}
