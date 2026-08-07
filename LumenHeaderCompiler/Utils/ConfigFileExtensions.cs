using System.Text;
using System.Text.Json;

namespace lhc;

internal static class ConfigFileExtensions {

    public static string GetTemplate( this ConfigFile cfg, string key ) {
        return cfg.templates.TryGetValue( key, out var val )
            ? val
            : throw new KeyNotFoundException( $"Missing template key '{key}' in config.json" );
    }

    public static Blueprint GetBlueprint( this ConfigFile cfg, string key, string separator ) {
        return cfg.blueprints.TryGetValue( key, out var val )
            ? new Blueprint( key, string.Join( separator, val ) )
            : throw new KeyNotFoundException( $"Missing blueprint key '{key}' in config.json" );
    }

    public static string GetDefault( this ConfigFile cfg, string key ) {
        return cfg.defaults.TryGetValue( key, out var val )
            ? val
            : throw new KeyNotFoundException( $"Missing defaults key '{key}' in config.json" );
    }

    public static string? TypeToReader( this ConfigFile cfg, string type ) {
        if (cfg.mTypesCfg.types.TryGetValue( type, out var value )) {
            return value.reader;
        }
        return null;
    }

    public static string? TypeToWriter( this ConfigFile cfg, string type ) {
        if (cfg.mTypesCfg.types.TryGetValue( type, out var value )) {
            return value.writer;
        }
        return null;
    }

    public static string ResolveFilePreamble( this ConfigFile cfg, string? sourceFile = null, bool pragmaOnce = true, IEnumerable<string>? extraIncludes = null ) {

        string sourceName = sourceFile != null ? Path.GetFileName( sourceFile ) : "";
        Blueprint formattedPreamble = cfg
            .GetBlueprint( "file_preamble", "\n" )
            .FormatWith( "Source", sourceName );

        StringBuilder sb = new( );
        sb.AppendLine( formattedPreamble.mContent );
        if (pragmaOnce) sb.AppendLine( "#pragma once" );

        if (sourceFile != null) sb.AppendLine( $"#include \"{Path.GetFileName( sourceFile )}\"" );
        if (extraIncludes != null) {
            foreach (var include in extraIncludes.Distinct( )) {
                sb.AppendLine( $"#include \"{include.Replace( '\\', '/' )}\"" );
            }
        }

        return sb.ToString( );

    }

    public static string? TypeToInspector( this TypesConfigFile cfg, string type ) {
        return cfg.types.TryGetValue( type, out var val )
            ? string.Join( '\n', val.inspector )
            : throw new KeyNotFoundException( $"Unknown type '{type}' in config.json" );
    }

    public static string TypeToDroppableInspector( this TypesConfigFile cfg, string type ) {
        if (!cfg.types.TryGetValue( type, out var val )) {
            throw new KeyNotFoundException( $"Unknown type '{type}' in config.json" );
        }

        var target = val.droppable_inspector ?? val.inspector;
        if (target == null) {
            throw new KeyNotFoundException( $"Type '{type}' has no inspector code defined in config.json" );
        }

        return string.Join( '\n', target );
    }

}