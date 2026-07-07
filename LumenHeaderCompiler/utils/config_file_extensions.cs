using System.Text;

namespace lhc;

internal static class ConfigFileExtensions {

    public static string GetTemplate( this ConfigFile cfg, string key ) {
        return cfg.templates.TryGetValue( key, out var val )
            ? val
            : throw new KeyNotFoundException( $"Missing template key '{key}' in config.json" );
    }

    public static List<string> GetBlueprint( this ConfigFile cfg, string key ) {
        return cfg.blueprints.TryGetValue( key, out var val )
             ? val
             : throw new KeyNotFoundException( $"Missing function template key '{key}' in config.json" );
    }

    public static string GetDefault(this ConfigFile cfg, string key) {
        return cfg.defaults.TryGetValue( key, out var val )
            ? val
            : throw new KeyNotFoundException( $"Missing defaults key '{key}' in config.json" );
    }

    public static string? TypeToInspector( this ConfigFile cfg, string type ) {
        return cfg.types.TryGetValue( type, out var val)
            ? string.Join('\n', val.inspector)
            : throw new KeyNotFoundException( $"Unknown type '{type}' in config.json" );
    }

    public static string TypeToDroppableInspector( this ConfigFile cfg, string type ) {
        if(cfg.types.TryGetValue( type, out var val )) {
            var target = val.droppable_inspector ?? val.inspector;

            if(target == null)
                throw new KeyNotFoundException( $"Type '{type}' has no inspector code defined in config.json" );

            return string.Join( '\n', target );
        }
        throw new KeyNotFoundException( $"Unknown type '{type}' in config.json" );
    }

    public static string? TypeToReader( this ConfigFile cfg, string type ) {
        if (cfg.types.TryGetValue( type, out var value )) {
            return value.reader;
        }
        return null;
    }

    public static string ResolveFilePreamble( this ConfigFile cfg, string? sourceFile, IEnumerable<string>? extraIncludes = null ) {

        List<string> preamble = cfg.GetBlueprint( "file_preamble" );

        string sourceName = sourceFile != null ? Path.GetFileName( sourceFile ) : "";
        string formattedPreamble = string.Join( '\n', preamble ).FormatWith( "Source", sourceName );

        StringBuilder sb = new( );
        sb.AppendLine( formattedPreamble );

        if (sourceFile != null) sb.AppendLine( $"#include \"{Path.GetFileName(sourceFile)}\"" );
        if(extraIncludes != null) {
            foreach( var include in extraIncludes.Distinct()) {
                sb.AppendLine( $"#include \"{include.Replace( '\\', '/' )}\"" );
            }
        }

        return sb.ToString( );

    }

}