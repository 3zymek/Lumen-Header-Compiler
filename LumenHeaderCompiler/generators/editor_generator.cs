using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace lhc;

internal static class EditorGenerator {

    private static StringBuilder mSbuilder = new( );
    private static StringBuilder mOutputBuilder = new( );
    private static HashSet<string> mIncludes = new( );

    public static void GenerateEditorFn( ClassGeneratedInfo info ) {

        string signature = string.Format( HeaderGenerator.GetTemplate( "editor_fn_signature" ), info.mInfo.mTypeName );
        mSbuilder.AppendLine( $"\tinline void {signature}" + " {\n" );
        string classVarName = HeaderGenerator.GetTemplate( "editor_fn_comp_name" );
        string getter = string.Format(
            HeaderGenerator.GetTemplate( "editor_fn_comp_getter" ),
            classVarName,
            info.mInfo.mTypeName
        );

        mSbuilder.AppendLine( $"\t\t{getter}" );

        string check = string.Format( HeaderGenerator.GetTemplate( "editor_fn_getter_check" ), classVarName );
        mSbuilder.AppendLine( $"\t\t{check}" );

        foreach (var field in info.mInfo.mFields) {

            string inspector = HeaderGenerator.TypeToInspector( field.mType ) ??
               throw new Exception( $"Unknown type: '{field.mType}' in {info.mInfo.mTypeName}.{field.mName}" );

            mSbuilder.AppendLine( $"\t\t{string.Format( inspector, classVarName, field.mName )};" );

        }

        mSbuilder.AppendLine( "\n\t}\n" );

        mIncludes.Add( info.mOriginalFilepath );

    }

    public static void Finalize( string root, Dictionary<string, ClassGeneratedInfo> components ) {

        string editorDepMgrPath = Path.Combine( root, HeaderGenerator.GetPath( "editor_dep_manager" ) );

        string editorFnNamespace = HeaderGenerator.GetTemplate( "editor_fn_namespace" );
        if (!File.Exists( editorDepMgrPath )) throw new Exception( $"Path to editor dependency manager is invalid: {editorDepMgrPath}" );

        var relativeIncludes = components.Values
            .Select( v => v.mGeneratedFilepath )
            .Distinct( )
            .Select( absPath => Path.GetRelativePath( Path.GetDirectoryName( editorDepMgrPath )!, absPath ) );

        HeaderGenerator.GeneratePreamble( mOutputBuilder, editorDepMgrPath, relativeIncludes );

        mOutputBuilder.AppendLine( $"namespace {editorFnNamespace}" + " {\n" );

        generate_editor_registry( mOutputBuilder, components );
        mOutputBuilder.Append( mSbuilder.ToString( ) );

        mOutputBuilder.AppendLine( "} " + $"// namespace {editorFnNamespace}\n" ); // namespace

        string outputPath = Path.Combine(
            Path.GetDirectoryName( editorDepMgrPath )!,
            Path.GetFileNameWithoutExtension( editorDepMgrPath ) + ".generated.hpp"
        );

        File.WriteAllText( outputPath, mOutputBuilder.ToString( ) );

    }

    private static void generate_editor_registry( StringBuilder sb, Dictionary<string, ClassGeneratedInfo> components ) {

        string mapName = "map";
        sb.AppendLine( $"\tinline void {string.Format( HeaderGenerator.GetTemplate( "editor_fn_registry" ), mapName )}" + " {" );

        foreach (var (key, val) in components) {
            sb.AppendLine( $"\t\t{mapName}[ HashStr( \"{HeaderGenerator.GetClassName( val.mInfo )}\" ) ] = {val.mEditorFnName};" );
        }

        sb.AppendLine( "\t}\n" ); // function

    }

}
