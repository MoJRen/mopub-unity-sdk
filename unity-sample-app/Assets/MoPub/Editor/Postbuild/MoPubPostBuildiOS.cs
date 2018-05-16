#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;

using UnityEngine;

// ReSharper disable once CheckNamespace
namespace MoPubInternal.Editor.Postbuild
{
    public static class MoPubPostBuildiOS
    {
        private static readonly string[] PlatformLibs = { "libz.dylib", "libsqlite3.dylib", "libxml2.dylib" };

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {
            // BuiltTarget.iOS is not defined in Unity 4, so we just use strings here
            if (buildTarget.ToString() != "iOS" && buildTarget.ToString() != "iPhone") return;
            CheckiOSVersion();
            PrepareProject(buildPath);
            RenameMRAIDSource(buildPath);
        }

        private static void CheckiOSVersion()
        {
            var iOSTargetVersion = double.Parse(PlayerSettings.iOS.targetOSVersionString);
            if (iOSTargetVersion < 7) {
                Debug.LogWarning("MoPub requires iOS 7+. Please change the Target iOS Version in Player Settings to " +
                                 "iOS 7 or higher.");
            }
        }

        private static void PrepareProject(string buildPath)
        {
            var projPath = Path.Combine(buildPath, "Unity-iPhone.xcodeproj/project.pbxproj");
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projPath));
            var target = project.TargetGuidByName("Unity-iPhone");

            foreach (var lib in PlatformLibs) {
                string libGUID = project.AddFile("usr/lib/" + lib, "Libraries/" + lib, PBXSourceTree.Sdk);
                project.AddFileToBuild(target, libGUID);
            }

            var fileGuid = project.FindFileGuidByProjectPath("Frameworks/Plugins/iOS/MoPubSDKFramework.framework");
            project.AddFileToEmbedFrameworks(target, fileGuid);
            project.SetBuildProperty(
                target, "LD_RUNPATH_SEARCH_PATHS", "$(inherited) @executable_path/Frameworks");

            project.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");
            project.AddBuildProperty(target, "CLANG_ENABLE_MODULES", "YES");
            project.AddBuildProperty(target, "ENABLE_BITCODE", "NO");

            File.WriteAllText(projPath, project.WriteToString());
        }

        private static void RenameMRAIDSource(string buildPath)
        {
            // Unity will try to compile anything with the ".js" extension. Since mraid.js is not intended
            // for Unity, it'd break the build. So we store the file with a masked extension and after the
            // build rename it to the correct one.

            var maskedFiles = Directory.GetFiles(
                buildPath, "*.prevent_unity_compilation", SearchOption.AllDirectories);
            foreach (var maskedFile in maskedFiles) {
                var unmaskedFile = maskedFile.Replace(".prevent_unity_compilation", "");
                File.Move(maskedFile, unmaskedFile);
            }
        }
    }
}
#endif