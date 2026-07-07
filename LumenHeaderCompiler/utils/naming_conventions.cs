
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

    public static string ResolveParseName( this ClassInfo info ) {
        string fallback = info.mTypeName.StartsWith( 'C' ) ? info.mTypeName[1..] : info.mTypeName;
        fallback = System.Text.RegularExpressions.Regex.Replace( fallback, "([A-Z])", "_$1" ).TrimStart( '_' ).ToLower( );
        return info.mArgs.mParseName ?? fallback;
    }

    public static string ResolveDisplayName( this ClassInfo info ) {
        string fallback = info.mTypeName.StartsWith( 'C' ) ? info.mTypeName[1..] : info.mTypeName;
        return info.mArgs.mDisplayName ?? camel_case_to_display( fallback );
    }

    public static string ResolveCategoryName( this ClassInfo info, string fallback ) {
        return info.mArgs.mCategoryName ?? fallback;
    }
    
    public static string ResolveDisplayName( this FieldInfo info, ConfigFile cfg ) {
        string name = info.mName;
        if (name.Length > 1 && cfg.prefixes.Contains( name[0].ToString( ) ) && char.IsUpper( name[1] )) {
            name = name.Substring( 1 );
        }
        return info.mArgs.mDisplayName ?? camel_case_to_display( name );
    }
    public static string ResolveFieldName( this FieldInfo info, ConfigFile cfg ) {
        string name = info.mName;
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

        return Path.Combine( directory, $"{fileNameWithoutExt}.generated.{extension}" );
    }

}