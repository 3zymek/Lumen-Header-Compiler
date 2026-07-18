using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace lhc;

internal static class JsonExtensions {

    public static T TryDeserialize<T>( this string json, JsonSerializerOptions? options = null, string? errorMsg = null ) {

        try {
            return JsonSerializer.Deserialize<T>( json, options )
                ?? throw new JsonException( "Deserialization returned null." );
        }
        catch(Exception ex) {
            throw new InvalidOperationException(
                errorMsg ?? $"Failed to deserialize JSON to type {typeof( T ).Name}.", ex );
        }

    }

}
