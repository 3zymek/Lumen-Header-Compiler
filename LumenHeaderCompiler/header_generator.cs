using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace lhc;

internal record JsonProperty( string reader, string inspector );
internal record JsonRoot(
    Dictionary<string, string> templates,
    Dictionary<string, JsonProperty> types
    );

internal record ClassGeneratedInfo(
    ClassInfo mInfo,
    string mGeneratedFilepath,
    string mParseFnName,
    string mSerializeFnName,
    string mEditorFnName
    );

internal static class HeaderGenerator {

    private static JsonRoot? mRoot;
    private static Dictionary<string, ClassGeneratedInfo> mComponents = new( );

    private static string mSceneManagerPath = "";

    public static void Initialize( string sceneDepMgrPath ) {

        string jsonContent = File.ReadAllText( $"{Path.Combine( AppContext.BaseDirectory, "config.json" )}" );

        mRoot = JsonSerializer.Deserialize<JsonRoot>( jsonContent ) ??
            throw new Exception( $"Failed to deserialize {Path.Combine( AppContext.BaseDirectory, "config.json" )}" );

        mSceneManagerPath = sceneDepMgrPath.Replace( '\\', '/' );

    }

    public static void GenerateFile( string sourceFile, List<ClassInfo> components ) {

        if (!File.Exists( sourceFile )) { throw new Exception( $"File {sourceFile} doesn't exist" ); }
        if (mRoot == null) throw new Exception( "Header generator not initialized" );

        StringBuilder sb = new( );
        string generatedPath = Path.Combine(
            Path.GetDirectoryName( sourceFile )!,
            Path.GetFileNameWithoutExtension( sourceFile ) + ".generated.hpp"
        );

        generate_preamble( sb, sourceFile, generatedPath );
        foreach (var comp in components) {

            string compName = get_component_key( comp );
            string parseFnSig = mRoot!.templates["parse_fn_signature"];
            string parseFnName = parseFnSig.Substring( 0, parseFnSig.IndexOf( '(' ) );

            string editorFnSig = mRoot!.templates["editor_fn_signature"];
            string editorFnName = editorFnSig.Substring( 0, editorFnSig.IndexOf( '(' ) );

            mComponents[compName] = new ClassGeneratedInfo(
                mInfo: comp,
                mGeneratedFilepath: generatedPath,
                mParseFnName: string.Format( parseFnName, comp.mTypeName ),
                mSerializeFnName: "TO IMPLEMENT",
                mEditorFnName: string.Format( editorFnName, comp.mTypeName )
                );

            generate_parse_fn( sb, comp );
            generate_editor_fn( sb, comp );
            generate_component_name_fn( sb, comp );

        }

        File.WriteAllText( generatedPath, sb.ToString( ) );

    }

    public static void Finalize( string outputDir ) {

        if (!File.Exists( outputDir )) throw new Exception( $"File {outputDir} doesn't exist" );
        if (mRoot == null) throw new Exception( "Header generator not initialized" );

        string outputPath = Path.Combine(
            Path.GetDirectoryName( outputDir )!,
            Path.GetFileNameWithoutExtension( outputDir ) + ".generated.hpp"
        );

        StringBuilder sb = new( );
        generate_preamble( sb, null, outputPath, mComponents.Values.Select( v => v.mGeneratedFilepath ).Distinct( ) );

        finalize_parse_map( sb, outputPath );
        finalize_inspector_map( sb, outputPath );

        File.WriteAllText( outputPath, sb.ToString( ) );

    }

    private static string get_component_key( ClassInfo component ) {
        string fallback = component.mTypeName.StartsWith( 'C' )
            ? component.mTypeName[1..].ToLower( ) : component.mTypeName.ToLower( );
        return component.mArgs.mID ?? fallback;
    }

    private static void finalize_parse_map( StringBuilder sb, string outputPath ) {

        string parseFnNamespace = mRoot!.templates["parse_fn_namespace"];
        sb.AppendLine( $"namespace {parseFnNamespace}" + " {\n" );
        sb.Append( $"\tinline void {mRoot!.templates["parse_fn_registry"]} " );
        sb.AppendLine( "{" );

        foreach (var (key, val) in mComponents) {

            sb.AppendLine( $"\t\tmap[ HashStr(\"{val.mInfo.mArgs.mID ?? key}\") ] = {val.mParseFnName};" );

        }

        sb.AppendLine( "\t}\n" ); // function
        sb.AppendLine( "} " + $"// namespace {parseFnNamespace}\n" ); // namespace

    }

    public static void finalize_inspector_map( StringBuilder sb, string outputPath ) {

        string compNameNamespace = mRoot!.templates["component_get_name_namespace"];
        sb.AppendLine( $"namespace {compNameNamespace}" + " {\n" );
        sb.Append( $"\tinline void {mRoot!.templates["editor_fn_registry"]}" );
        sb.AppendLine( " {\n" );

        foreach (var (key, val) in mComponents) {

            sb.AppendLine( $"\t\tmap[ HashStr(\"{val.mInfo.mArgs.mID ?? key}\") ] = {val.mEditorFnName};" );

        }

        sb.AppendLine( "\t}\n" );
        sb.AppendLine( "} " + $"// namespace {compNameNamespace}" );

    }

    private static string? type_to_inspector( string type ) {
        if (mRoot!.types.TryGetValue( type, out var value )) {
            return value.inspector;
        }
        return null;
    }

    private static string? type_to_reader( string type ) {
        if (mRoot!.types.TryGetValue( type, out var value )) {
            return value.reader;
        }
        return null;
    }

    private static void generate_preamble( StringBuilder sb, string? sourceFile, string? generatedPath = null, IEnumerable<string>? extraIncludes = null ) {

        sb.AppendLine( $"//========= Copyright (C) 2026 3zymek, MIT License ============//" );
        sb.AppendLine( $"//" );
        sb.AppendLine( $"// Auto-generated by Lumen Header Compiler (LHC)." );
        sb.AppendLine( $"// Repository: https://github.com/3zymek/Lumen-Header-Compiler" );
        sb.AppendLine( $"// Source: {Path.GetFileName( sourceFile )}" );
        sb.AppendLine( $"//" );
        sb.AppendLine( $"// DO NOT EDIT - changes will be overwritten on next build." );
        sb.AppendLine( $"//" );
        sb.AppendLine( $"//=============================================================================//" );
        sb.AppendLine( $"#pragma once" );
        if (sourceFile != null) sb.AppendLine( $"#include \"{Path.GetFileName( sourceFile )}\"" );
        string sceneInclude = generatedPath != null
            ? Path.GetRelativePath( Path.GetDirectoryName( generatedPath )!, mSceneManagerPath ).Replace( '\\', '/' )
            : mSceneManagerPath;
        sb.AppendLine( $"#include \"{sceneInclude}\"" );

        if (extraIncludes != null) {
            foreach (var include in extraIncludes) {
                sb.AppendLine( $"#include \"{include.Replace( '\\', '/' )}\"" );
            }
        }
        sb.AppendLine( );

    }

    private static void generate_component_name_fn( StringBuilder sb, ClassInfo component ) {

        string compNameNamespace = mRoot!.templates["component_get_name_namespace"];
        sb.AppendLine( $"namespace {compNameNamespace}" + " {\n" );

        sb.AppendLine( "\ttemplate<>" );
        sb.AppendLine(
            "\tinline " +
            mRoot!.templates["component_get_name_return"] +
            " " +
            string.Format( mRoot!.templates["component_get_name_signature"], component.mTypeName ) +
            " {\n"
            );
        sb.AppendLine( $"\t\treturn \"{get_component_key( component )}\";\n" );
        sb.AppendLine( "\t}\n" );

        sb.AppendLine( "} " + $"// namespace {compNameNamespace}" );

    }

    private static void generate_editor_fn( StringBuilder sb, ClassInfo component ) {

        string editorFnNamespace = mRoot!.templates["editor_fn_namespace"];
        sb.AppendLine( $"namespace {editorFnNamespace}" + " {\n" );

        string signature = string.Format( mRoot!.templates["editor_fn_signature"], component.mTypeName );
        sb.Append( "\t" + "inline void " + signature );
        sb.AppendLine( " { \n" );

        string compName = mRoot!.templates["editor_fn_comp_name"];

        string getter = string.Format(
            mRoot!.templates["editor_fn_comp_getter"],
            compName,
            component.mTypeName
            );

        sb.AppendLine( "\t\t" + getter );

        string check = string.Format( mRoot!.templates["editor_fn_getter_check"]?.ToString( ) ?? "", compName );
        sb.AppendLine( "\t\t" + check );

        foreach (var field in component.mFields) {

            string inspector = type_to_inspector( field.mType ) ??
                throw new Exception( $"Unknown type: '{field.mType}' in {component.mTypeName}.{field.mName}" );

            string format = string.Format( inspector, compName, field.mName );
            sb.AppendLine( "\t\t" + format + ';' );

        }

        sb.AppendLine( "\n\t}\n" ); // function
        sb.AppendLine( "} " + $"// namespace {editorFnNamespace}\n" ); // namespace

    }

    private static void generate_parse_fn( StringBuilder sb, ClassInfo component ) {

        string parseFnNamespace = mRoot!.templates["parse_fn_namespace"];
        sb.AppendLine( $"namespace {parseFnNamespace}" + " {\n" );

        string compName = component.mTypeName.StartsWith( 'C' ) ? component.mTypeName[1..].ToLower( ) : component.mTypeName.ToLower( );
        string componentAlias = mRoot!.templates["parse_fn_comp_name"]!;

        sb.Append( $"\tinline void {string.Format( mRoot.templates["parse_fn_signature"], component.mTypeName )}" );
        sb.AppendLine( " {\n" );
        sb.AppendLine( $"\t\t{mRoot.templates["parse_fn_body_open"]}\n" );
        sb.AppendLine( $"\t\t{component.mTypeName} {componentAlias}; \n" );
        sb.Append( $"\t\twhile( {mRoot.templates["parse_fn_while_expr"]} )" );
        sb.AppendLine( " {" );
        sb.Append( $"\t\t\tif( {mRoot.templates["parse_fn_token_check"]} )" );
        sb.AppendLine( "{" );

        for (int i = 0; i < component.mFields.Count; i++) {

            var field = component.mFields[i];
            string reader = type_to_reader( field.mType ) ??
                throw new Exception( $"Unknown type: '{field.mType}' in {component.mTypeName}.{field.mName}" );

            string keyword = i == 0 ? "if" : "else if";
            string fieldName = field.mName.TrimStart( 'm' ).ToLower( );

            sb.AppendLine( $"\t\t\t\t{keyword}( detail::IsString( tokens, i, \"{fieldName}\" ) )" );
            sb.AppendLine( $"\t\t\t\t\t{string.Format( mRoot.templates["parse_fn_field"], componentAlias, field.mName, reader )}" );

        }

        sb.AppendLine( "\t\t\t}" );
        sb.AppendLine( "\t\t\ti++;" );
        sb.AppendLine( "\t\t}\n" );
        sb.AppendLine( $"\t\t{string.Format( mRoot.templates["parse_fn_add"], componentAlias )}\n" );
        sb.AppendLine( "\t}\n" ); // function
        sb.AppendLine( "} " + $"// namespace {parseFnNamespace}\n" ); // namespace

    }

}
