using lhc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace lhc;

internal class SupportedMacros {
    public required string class_macro { get; init; }
    public required string property_macro { get; init; }
    public required string function_macro { get; init; }
    public required string generated_body_macro { get; init; }
    public required string class_extensions_macro { get; init; }

    [JsonIgnore]
    public List<string> mAll => [
        class_macro,
        property_macro,
        function_macro,
        generated_body_macro,
        class_extensions_macro
    ];

}

internal record TypeProperties(
    string reader,
    string writer,
    List<string> inspector,
    List<string>? droppable_inspector
);
internal class TypesConfigFile {
    public required Dictionary<string, TypeProperties> types { get; init; }
}

internal record OutputProperties(
    string registry_type,
    string finalize_path
    );

internal record ConfigFile(
    List<OutputProperties> outputs,
    SupportedMacros supported_macros,
    Dictionary<string, string> category_colors,
    Dictionary<string, string> category_icons,
    List<string> prefixes,
    Dictionary<string, List<string>> blueprints,
    Dictionary<string, string> templates,
    Dictionary<string, string> defaults
) {
    [JsonIgnore] public TypesConfigFile mTypesCfg { get; set; } = null!;
}

