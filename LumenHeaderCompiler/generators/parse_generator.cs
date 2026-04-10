using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace lhc;

internal static class ParseGenerator {

    public static void GenerateParseFn( StringBuilder sb, ClassInfo info ) {

        string parseFnNamespace = HeaderGenerator.GetTemplate( "parse_fn_namespace" );

        sb.AppendLine( $"namespace {parseFnNamespace}" + " {\n" );

        string className = HeaderGenerator.GetClassName( info );
        string classVarName = HeaderGenerator.GetTemplate( "parse_fn_var_name" );
        string parseFnSig = HeaderGenerator.GetTemplate( "parse_fn_signature" ).FormatWith( "ClassName", info.mTypeName );

        sb.AppendLine( $"\tinline void {parseFnSig}" + " {\n" );
        sb.AppendLine( $"\t\t{HeaderGenerator.GetTemplate( "parse_fn_body_open" )}" );
        sb.AppendLine( $"\t\t{info.mTypeName} {classVarName};" );
        sb.AppendLine( $"\t\twhile( {HeaderGenerator.GetTemplate( "parse_fn_while_expr" )} )" + " {\n" );
        sb.AppendLine( $"\t\t\tif( {HeaderGenerator.GetTemplate( "parse_fn_token_check" )} )" + " {\n" );
        for (int i = 0; i < info.mFields.Count; i++) {

            FieldInfo field = info.mFields[i];
            string reader = HeaderGenerator.TypeToReader( field.mType ) ??
                throw new Exception( $"Unknown type: '{field.mType}' in {info.mTypeName}.{field.mName}" );

            string keyword = i == 0 ? "if" : "else if";
            string fieldName = HeaderGenerator.GetFieldName( field );
            string statement = $"{keyword}( {HeaderGenerator.GetTemplate( "parse_fn_while_statement" ).FormatWith( "Value", fieldName )}";

            sb.AppendLine( $"\t\t\t\t{statement} )" );
            string fieldFormatted = HeaderGenerator.GetTemplate( "parse_fn_field" ).FormatWith( new Dictionary<string, string> {
                { "Var", classVarName },
                { "FieldName", field.mName },
                { "Reader", reader }
            } );

            sb.AppendLine( $"\t\t\t\t\t{fieldFormatted}" );

        }

        sb.AppendLine( "\t\t\t}" );
        sb.AppendLine( "\t\t\ti++;" );
        sb.AppendLine( "\t\t}\n" );
        sb.AppendLine( $"\t\t{HeaderGenerator.GetTemplate( "parse_fn_add" ).FormatWith( "Var", classVarName )}\n" );
        sb.AppendLine( "\t}\n" ); // function
        sb.AppendLine( "} " + $"// namespace {parseFnNamespace}\n" ); // namespace

    }

    public static void Finalize( string root, Dictionary<string, ClassGeneratedInfo> components ) {

        string sceneDepMgrPath = Path.Combine( root, HeaderGenerator.GetPath( "scene_dep_manager_path" ) );
        string sceneDepMgrInclude = HeaderGenerator.GetPath( "scene_dep_manager_include" );
        string parseFnNamespace = HeaderGenerator.GetTemplate( "parse_fn_namespace" );

        if (!File.Exists( sceneDepMgrPath )) throw new Exception( "Scene dependency manager path is invalid" );

        StringBuilder sb = new( );
        var relativeIncludes = components.Values
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( sceneDepMgrPath )!, absPath ) );

        HeaderGenerator.GeneratePreamble( sb, null, new[] { sceneDepMgrInclude }.Concat( relativeIncludes ) );

        string mapName = "map";
        sb.AppendLine( $"namespace {parseFnNamespace}" + " {\n" );
        sb.AppendLine( $"\tinline void {HeaderGenerator.GetTemplate( "parse_fn_registry" ).FormatWith( "Param", mapName )}" + " {" );

        foreach (var (key, val) in components) {
            sb.AppendLine( $"\t\t{mapName}[ HashStr(\"{HeaderGenerator.GetClassName( val.mInfo )}\") ] = {val.mParseFnName};" );
        }

        sb.AppendLine( "\t}\n" ); // function
        sb.AppendLine( "} " + $"// namespace {parseFnNamespace}\n" ); // namespace

        string outputPath = Path.Combine(
            Path.GetDirectoryName( sceneDepMgrPath )!,
            Path.GetFileNameWithoutExtension( sceneDepMgrPath ) + ".generated.hpp"
        );

        File.WriteAllText( outputPath, sb.ToString( ) );

    }

}
