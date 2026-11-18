// Editor helpers: makes sure a Main scene exists and is in the build, keeps the
// runtime shaders in every build, and adds a "Phewa Valley" menu.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PhewaValley.EditorTools
{
    [InitializeOnLoad]
    public static class PhewaSetup
    {
        const string ScenePath = "Assets/PhewaValley/Scenes/Main.unity";
        static readonly string[] RuntimeShaders = { "PhewaValley/VertexColorStandard", "PhewaValley/SignText", "PhewaValley/Lake", "Standard", "Skybox/Procedural", "Sprites/Default", "glTF/PbrMetallicRoughness", "glTF/PbrSpecularGlossiness", "glTF/Unlit" };

        static PhewaSetup() { EditorApplication.delayCall += EnsureProject; }

        [MenuItem("Phewa Valley/Repair project setup")]
        public static void EnsureProject()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.Refresh();
                Debug.Log("[Phewa Valley] Created " + ScenePath + ". Press Play: the game builds itself at runtime.");
            }
            if (EditorBuildSettings.scenes.Length == 0 || EditorBuildSettings.scenes[0].path != ScenePath)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            IncludeShaders();
            PlayerSettings.colorSpace = ColorSpace.Linear; // realistic lighting; all colours are authored linear
            // Keep the post-processing shaders in builds via a Resources holder.
            const string fx = "Assets/PhewaValley/Resources/PhewaPostFX.asset";
            if (AssetDatabase.LoadAssetAtPath<PostFXResources>(fx) == null)
            {
                var guids = AssetDatabase.FindAssets("t:PostProcessResources");
                if (guids.Length > 0)
                {
                    Directory.CreateDirectory("Assets/PhewaValley/Resources");
                    var holder = ScriptableObject.CreateInstance<PostFXResources>();
                    holder.resources = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>(AssetDatabase.GUIDToAssetPath(guids[0]));
                    AssetDatabase.CreateAsset(holder, fx); AssetDatabase.SaveAssets();
                }
            }
            PlayerSettings.productName = "Phewa Valley"; PlayerSettings.companyName = "Aurora"; PlayerSettings.runInBackground = true;
        }

        static void IncludeShaders()
        {
            var graphics = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset"); if (!graphics) return;
            var so = new SerializedObject(graphics); var list = so.FindProperty("m_AlwaysIncludedShaders"); bool changed = false;
            foreach (var name in RuntimeShaders)
            {
                var shader = Shader.Find(name); if (!shader) continue;
                bool present = false; for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) present = true;
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader; changed = true;
            }
            if (changed) so.ApplyModifiedProperties();
        }

        [MenuItem("Phewa Valley/Open Main scene")]
        static void OpenMain() { EnsureProject(); EditorSceneManager.OpenScene(ScenePath); }

        [MenuItem("Phewa Valley/Open Tripo models folder")]
        static void OpenModels()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, "PhewaValley", "models"); Directory.CreateDirectory(dir); EditorUtility.RevealInFinder(dir);
        }

        // Noninteractive build used for repeatable controller checks and visual smoke tests.
        public static void BuildValidation()
        {
            EnsureProject();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Validation/PhewaValley.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("Phewa Valley/Build for Windows")]
        public static void BuildWindows()
        {
            EnsureProject();
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Windows", "PhewaValley.exe"));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            Debug.Log($"[Phewa Valley] Build {report.summary.result}: {output}");
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorUtility.RevealInFinder(output);
        }
    }
}
