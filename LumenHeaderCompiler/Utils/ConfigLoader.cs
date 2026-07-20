using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace lhc;

internal static class ConfigLoader {
    public static T LoadFromFile<T>( string relativePath, JsonSerializerOptions? options = null ) {

        string fullPath = Path.Combine( AppContext.BaseDirectory, relativePath );

        if (!File.Exists( fullPath )) {
            throw new FileNotFoundException( $"LHC Configuration file doesn't exist at: {fullPath}" );
        }

        string content = File.ReadAllText( fullPath );
        return content.TryDeserialize<T>( options, "Failed to load LHC configuration file." );


    }
}


