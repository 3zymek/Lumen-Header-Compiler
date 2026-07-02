namespace lhc;

internal static class ConfigFileExtensions {

    public static string GetPath( this ConfigFile cfg, string key ) {
        return cfg.paths.TryGetValue( key, out var val )
             ? val
             : throw new Exception( $"Missing path key '{key}' in config.json" );
    }
    public static string GetTemplate( this ConfigFile cfg, string key ) {
        return cfg.templates.TryGetValue( key, out var val )
            ? val
            : throw new Exception( $"Missing template key '{key}' in config.json" );
    }

    public static List<string> GetFunctionTemplate( this ConfigFile cfg, string key ) {
        return cfg.function_templates.TryGetValue( key, out var val )
             ? val
             : throw new Exception( $"Missing function template key '{key}' in config.json" );
    }

    public static string GetDefault(this ConfigFile cfg, string key) {
        return cfg.defaults.TryGetValue( key, out var val )
            ? val
            : throw new Exception( $"Missing defaults key '{key}' in config.json" );
    }

    public static string? TypeToInspector( this ConfigFile cfg, string type ) {
        return cfg.types.TryGetValue( type, out var val)
            ? string.Join('\n', val.inspector)
            : throw new Exception( $"Unknown type '{type}' in config.json" );
    }

    public static string TypeToDroppableInspector( this ConfigFile cfg, string type ) {
        if(cfg.types.TryGetValue( type, out var val )) {
            var target = val.droppable_inspector ?? val.inspector;

            if(target == null)
                throw new Exception( $"Type '{type}' has no inspector code defined in config.json" );

            return string.Join( '\n', target );
        }
        throw new Exception( $"Unknown type '{type}' in config.json" );
    }

    public static string? TypeToReader( this ConfigFile cfg, string type ) {
        if (cfg.types.TryGetValue( type, out var value )) {
            return value.reader;
        }
        return null;
    }
}