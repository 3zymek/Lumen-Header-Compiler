using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace lhc;

internal record ClassGeneratedInfo(
    ClassInfo mInfo,
    string mGeneratedFilepath,
    string mOriginalFilepath,
    string mParseFnName,
    string mSerializeFnName,
    string mEditorFnName
    );

internal static class HeaderGenerator {

    private static ConfigFile? mCfg;
    private static string? mRootDir;
    private static Dictionary<string, ClassGeneratedInfo> mComponents = new( );

    public static void Initialize( string root, ConfigFile config ) {

        mCfg = config;
        mRootDir = root;

    }

    public static void GenerateFile( string sourceFile, List<ClassInfo> components ) {

        if (!File.Exists( sourceFile )) { throw new Exception( $"File {sourceFile} doesn't exist" ); }
        if (mCfg == null) throw new Exception( "Header generator not initialized" );

        StringBuilder sb = new( );
        string generatedPath = Path.Combine(
            Path.GetDirectoryName( sourceFile )!,
            Path.GetFileNameWithoutExtension( sourceFile ) + ".generated.hpp"
        );

        GeneratePreamble( sb, sourceFile, new[] { GetPath( "scene_dep_manager_include" ) } );
        foreach (var info in components) {

            string compName = GetClassParseName( info );
            string parseFnSig = GetTemplate( "parse_fn_signature" );
            string parseFnName = parseFnSig.Substring( 0, parseFnSig.IndexOf( '(' ) );

            string editorFnSig = GetTemplate( "editor_fn_signature" );
            string editorFnName = editorFnSig.Substring( 0, editorFnSig.IndexOf( '(' ) );

            var generatedInfo = mComponents[compName] = new ClassGeneratedInfo(
                mInfo: info,
                mGeneratedFilepath: generatedPath,
                mOriginalFilepath: sourceFile,
                mParseFnName: parseFnName.FormatWith( "ClassName", info.mTypeName ),
                mSerializeFnName: "TO IMPLEMENT",
                mEditorFnName: editorFnName.FormatWith( "ClassName", info.mTypeName )
                );

            ParseGenerator.GenerateParseFn( sb, info );
            EditorGenerator.GenerateEditorFn( generatedInfo );

            FGenerateNameGetterArgs args = new( );
            args.mSignature = GetTemplate( "get_parse_name_signature" );
            args.mNamespace = GetTemplate( "get_parse_name_namespace" );
            args.mReturnType = GetTemplate( "get_parse_name_return" );
            args.mReturnVal = GetClassParseName( info );
            generate_name_getter_fn( sb, info, args );

            args.mSignature = GetTemplate( "get_display_name_signature" );
            args.mNamespace = GetTemplate( "get_display_name_namespace" );
            args.mReturnType = GetTemplate( "get_display_name_return" );
            args.mReturnVal = GetClassDisplayName( info );
            generate_name_getter_fn( sb, info, args );

            args.mSignature = GetTemplate( "get_category_name_signature" );
            args.mNamespace = GetTemplate( "get_category_name_namespace" );
            args.mReturnType = GetTemplate( "get_category_name_return" );
            args.mReturnVal = info.mArgs.mCategoryName ?? HeaderGenerator.GetDefault( "category" );
            generate_name_getter_fn( sb, info, args );

        }

        File.WriteAllText( generatedPath, sb.ToString( ) );

    }

    public static string FormatWith( this string template, Dictionary<string, string> values ) {
        string result = template;
        foreach (var pair in values) {
            result = result.Replace( $"{{{pair.Key}}}", pair.Value );
        }
        return result;
    }
    public static string FormatWith( this string template, string key, string value ) {
        return template.Replace( $"{{{key}}}", value );
    }

    public static void Finalize( ) {

        if (mCfg == null) throw new Exception( "Header generator not initialized" );

        StringBuilder sb = new( );

        ParseGenerator.Finalize( mRootDir!, mComponents );
        EditorGenerator.Finalize( mRootDir!, mComponents );

    }

    public static string GetPath( string key ) {
        return mCfg!.paths.TryGetValue( key, out var val )
            ? val
            : throw new Exception( $"Missing path key '{key}' in config.json" );
    }

    public static string GetTemplate( string key ) {
        return mCfg!.templates.TryGetValue( key, out var val )
            ? val
            : throw new Exception( $"Missing template key '{key}' in config.json" );
    }

    public static Dictionary<string, string> GetCategoryColors( ) {
        return mCfg!.category_colors;
    }

    public static string GetDefault( string key ) {
        return mCfg!.defaults.TryGetValue( key, out var val )
            ? val
            : throw new Exception( $"Missing defaults key '{key}' in config.json" );
    }

    private static string camel_case_to_display( string name ) {
        return System.Text.RegularExpressions.Regex.Replace( name, "([A-Z])", " $1" ).Trim( );
    }

    public static string GetClassParseName( ClassInfo info ) {
        string fallback = info.mTypeName.StartsWith( 'C' )
            ? info.mTypeName[1..] : info.mTypeName;
        fallback = System.Text.RegularExpressions.Regex.Replace( fallback, "([A-Z])", "_$1" )
            .TrimStart( '_' ).ToLower( );
        return info.mArgs.mParseName ?? fallback;
    }

    public static string GetClassDisplayName( ClassInfo info ) {
        string fallback = info.mTypeName.StartsWith( 'C' )
            ? info.mTypeName[1..] : info.mTypeName;
        return info.mArgs.mDisplayName ?? camel_case_to_display( fallback );
    }

    public static string GetFieldDisplayName( FieldInfo info ) {
        string name = info.mName;
        if (name.Length > 1 && mCfg!.prefixes.Contains( name[0].ToString( ) ) && char.IsUpper( name[1] )) {
            name = name.Substring( 1 );
        }
        return info.mArgs.mDisplayName ?? camel_case_to_display( name );
    }

    public static string GetFieldName( FieldInfo info ) {
        string name = info.mName;
        if (name.Length > 1 && mCfg!.prefixes.Contains( name[0].ToString( ) ) && char.IsUpper( name[1] )) {
            name = name.Substring( 1 );
        }
        return name.ToLower( ).Replace( ' ', '_' );
    }

    public static string? TypeToInspector( string type ) {
        if (mCfg!.types.TryGetValue( type, out var value )) {
            return value.inspector;
        }
        return null;
    }

    public static string? TypeToReader( string type ) {
        if (mCfg!.types.TryGetValue( type, out var value )) {
            return value.reader;
        }
        return null;
    }

    public static void GeneratePreamble( StringBuilder sb, string? sourceFile, IEnumerable<string>? extraIncludes = null ) {

        sb.AppendLine( $"//========= Copyright (C) 2025-present 3zymek, MIT License ============//" );
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
        if (extraIncludes != null) {
            foreach (var include in extraIncludes) {
                sb.AppendLine( $"#include \"{include.Replace( '\\', '/' )}\"" );
            }
        }
        sb.AppendLine( );

    }

    private struct FGenerateNameGetterArgs {

        public string mNamespace;
        public string mReturnType;
        public string mSignature;
        public string mReturnVal;

    }

    private static void generate_name_getter_fn( StringBuilder sb, ClassInfo info, FGenerateNameGetterArgs args ) {

        sb.AppendLine( $"namespace {args.mNamespace}" + " {\n" );
        sb.AppendLine( "\ttemplate<>" );
        sb.AppendLine(
            "\tinline " +
            args.mReturnType +
            " " +
            args.mSignature.FormatWith( "ClassName", info.mTypeName ) +
            " {"
            );
        sb.AppendLine( $"\t\treturn \"{args.mReturnVal}\";" );
        sb.AppendLine( "\t}\n" );
        sb.AppendLine( "} " + $"// namespace {args.mNamespace}" );

    }

}
