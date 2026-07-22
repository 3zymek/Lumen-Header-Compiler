
using System.Globalization;
using System.Text;

namespace lhc;

internal static class NamingHelpers {

    private static string camel_case_to_display( string name ) {
        return System.Text.RegularExpressions.Regex.Replace( name, "([A-Z])", " $1" ).Trim( );
    }

    public static string FormatWith( this string template, Dictionary<string, string> values) {
        string result = template;
        foreach(var pair in values) {
            result = result.Replace( $"{{{pair.Key}}}", pair.Value );
        }
        return result;
    }
    public static string FormatWith( this string template, string key, string value ) {
        return template.Replace( $"{{{key}}}", value );
    }

    public static string ResolveDeserializeName( this ClassInfo info ) {
        string fallback = info.mTypeName.StartsWith( 'C' ) ? info.mTypeName[1..] : info.mTypeName;
        fallback = System.Text.RegularExpressions.Regex.Replace( fallback, "([A-Z])", "_$1" ).TrimStart( '_' ).ToLower( );
        return info.mArgs.mParseName ?? fallback;
    }

    public static string ResolveDisplayName( this ClassInfo info ) {
        string fallback = info.mTypeName.StartsWith( 'C' ) ? info.mTypeName[1..] : info.mTypeName;
        return info.mArgs.mDisplayName ?? camel_case_to_display( fallback );
    }

    public static string ResolveCategoryName( this ClassInfo info, ConfigFile cfg ) {
        return info.mArgs.mCategoryName ?? 
            cfg.GetDefault("category") ??
                throw new ArgumentNullException("Missing \"category\" default value in config.json");
    }
    
    public static string ResolveDisplayName( this FieldInfo info, ConfigFile cfg ) {
        string name = info.mVariableName;
        if (name.Length > 1 && cfg.prefixes.Contains( name[0].ToString( ) ) && char.IsUpper( name[1] )) {
            name = name.Substring( 1 );
        }
        return info.mArgs.mDisplayName ?? camel_case_to_display( name );
    }
    public static string ResolveFieldName( this FieldInfo info, ConfigFile cfg ) {
        string name = info.mVariableName;
        if (name.Length > 1 && cfg.prefixes.Contains( name[0].ToString( ) ) && char.IsUpper( name[1] )) {
            name = name.Substring( 1 );
        }
        return name.ToLower( ).Replace( ' ', '_' );
    }

    public static string MakeGeneratedPath( this string sourceFile, string extension ) {
        if (string.IsNullOrEmpty( sourceFile ))
            throw new ArgumentException( "Source file path cannot be null or empty.", nameof( sourceFile ) );

        string directory = Path.GetDirectoryName( sourceFile ) ?? "";
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension( sourceFile );

        return Path.Combine( directory, $"{fileNameWithoutExt}.gen.{extension}" );
    }

    public static int FindTokenIndex( this string blueprint, string blueprintName, string token ) {
        int index = blueprint.IndexOf( token );
        if (index == -1) throw new ArgumentNullException( $"Couldn't find {token} token in {blueprintName}" );
        return index;
    }

    public static string CalculateIndent( this string str, int baseIndex) {
        string alignment = new( "" );
        for(int i = baseIndex - 1; i >= 0; i--) {
            char c = str[i];
            if (c == '\n' || c == '\r')
                break;
            if (char.IsWhiteSpace( c ))
                alignment = c + alignment;
            else break;
        }
        return alignment;
    }

    public static void AssertFile( this string path, string? failMsg = null ) {
        if(!File.Exists( path ))
            throw new FileNotFoundException( failMsg ?? $"Required file \"{path}\" not found" );
    }

    public static void AssertDirectory( this string path, string? failMsg = null ) {
        if (!Directory.Exists( path ))
            throw new FileNotFoundException( failMsg ?? $"Required directory \"{path}\" not found" );
    }

    public static string InjectToNamespace( this string baseStr, string namespaceName ) {

        StringBuilder sb = new( );
        sb.AppendLine( $"namespace {namespaceName} {{" );
        sb.AppendLine( "\t" + baseStr.Replace( "\n", "\n\t" ));
        sb.AppendLine( $"}} // namespace {namespaceName}" );
        return sb.ToString( );

    } 

    public static void EnsureDirectory( this string path ) {
        string? dir = Path.GetDirectoryName( path );
        if (dir != null)
            Directory.CreateDirectory( dir );
    }

    public static string HexToVector4( this string hex ) {
        hex = hex.TrimStart( '#' );
        float r = Convert.ToInt32( hex[0..2], 16 ) / 255.0f;
        float g = Convert.ToInt32( hex[2..4], 16 ) / 255.0f;
        float b = Convert.ToInt32( hex[4..6], 16 ) / 255.0f;
        return $"{r.ToString( "F2", CultureInfo.InvariantCulture )}f, {g.ToString( "F2", CultureInfo.InvariantCulture )}f, {b.ToString( "F2", CultureInfo.InvariantCulture )}f, 1.0f";
    }

    public static string ResolveDeserializeFunctionName( this string rawName, ConfigFile cfg ) {
        string ns = cfg.GetTemplate( "deserialize_namespace" );
        return string.IsNullOrEmpty( ns ) ? rawName : $"{ns}::{rawName}";
    }

    public static string ResolveEditorFunctionName( this string rawName, ConfigFile cfg ) {
        string ns = cfg.GetTemplate( "editor_namespace" );
        return string.IsNullOrEmpty( ns ) ? rawName : $"{ns}::{rawName}";
    }

}