using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace lhc;

internal static class ParseGenerator {

    public static void GenerateParseFn( StringBuilder sb, ClassInfo info ) {

        string varName = HeaderGenerator.GetTemplate( "parse_fn_var" );
        string parseFn = string.Join( '\n', HeaderGenerator.GetFunctionTemplate( "parse_function" ) ).FormatWith( new( ) {
            { "ClassName", info.mTypeName },
            { "Var", varName }
        } );

        int index = parseFn.IndexOf( "{Fields}" );

        if (index != -1) {

            string alignment = new( "" );
            for (int i = index - 1; i >= 0; i--) {

                char c = parseFn[i];

                if (c == '\n' || c == '\r')
                    break;

                if (char.IsWhiteSpace( c )) {
                    alignment = c + alignment;
                }
                else break;

            }


            string preFields = parseFn.Substring( 0, index );
            string postFields = parseFn.Substring( index + "{Fields}".Length );

            StringBuilder sbStatements = new( );
            for (int i = 0; i < info.mFields.Count; i++) {

                FieldInfo field = info.mFields[i];
                string reader = HeaderGenerator.TypeToReader( field.mType ) ??
                    throw new Exception( $"Unknown type: '{field.mType}' in {info.mTypeName}.{field.mName}" );

                List<string> template = i == 0 ? HeaderGenerator.GetFunctionTemplate( "parse_fn_field_first" ) : HeaderGenerator.GetFunctionTemplate( "parse_fn_field_next" );
                string mergedTemplate = string.Join( '\n', template );

                string formattedBlock = mergedTemplate.FormatWith( new( )
                    {
                        { "FieldName", field.mName },
                        { "Var", varName },
                        { "Reader", reader },
                    }
                );

                if (i > 0)
                    sbStatements.Append( alignment );

                formattedBlock = formattedBlock.Replace( "\n", "\n" + alignment );
                sbStatements.AppendLine( formattedBlock );

            }

            string result = preFields + sbStatements.ToString( ) + postFields;
            sb.AppendLine( result );

        }
        else throw new Exception( $"Couldn't find Fields parameter in parse_function" );

    }

    public static void Finalize( string root, Dictionary<string, ClassGeneratedInfo> components ) {

        string sceneDepMgrPath = Path.Combine( root, HeaderGenerator.GetPath( "scene_dep_manager_path" ) );
        string sceneDepMgrInclude = HeaderGenerator.GetPath( "scene_dep_manager_include" );
        string parseFnNamespace = HeaderGenerator.GetTemplate( "parse_fn_namespace" );

        if (!File.Exists( sceneDepMgrPath )) throw new Exception( $"Scene dependency manager path is invalid: {sceneDepMgrPath}" );

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
            sb.AppendLine( $"\t\t{mapName}[ HashString(\"{HeaderGenerator.ResolveClassParseName( val.mInfo )}\") ] = {val.mParseFnName};" );
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
