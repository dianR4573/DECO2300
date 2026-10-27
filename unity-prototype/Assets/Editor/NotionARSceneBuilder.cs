#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.OpenXR;

public sealed class MetaQuestPassthroughBuildHooks : OpenXRFeatureBuildHooks
{
    public override int callbackOrder => 3;

    public override Type featureType => typeof(MetaQuestPassthroughFeature);

    protected override void OnPreprocessBuildExt(BuildReport report)
    {
    }

    protected override void OnPostGenerateGradleAndroidProjectExt(string path)
    {
    }

    protected override void OnPostprocessBuildExt(BuildReport report)
    {
    }

    protected override ManifestRequirement ProvideManifestRequirementExt()
    {
        return new ManifestRequirement
        {
            SupportedXRLoaders = new HashSet<Type> { typeof(OpenXRLoader) },
            NewElements = new List<ManifestElement>
            {
                new ManifestElement
                {
                    ElementPath = new List<string> { "manifest", "uses-feature" },
                    Attributes = new Dictionary<string, string>
                    {
                        { "name", "com.oculus.feature.PASSTHROUGH" },
                        { "required", "false" }
                    }
                }
            }
        };
    }
}

public static class NotionARSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainScene_NotionAR.unity";
    private const string AndroidPackageId = "com.deco2300.notionar";

    [MenuItem("Tools/DECO2300/Rebuild Notion AR Main Scene")]
    public static void RebuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainScene_NotionAR";

        // No camera or light: NotionARPrototype creates the head-tracked rig, and its shaders light themselves.
        GameObject controller = new GameObject("Notion AR Prototype Controller");
        controller.AddComponent<NotionARPrototype>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "DECO2300",
                "MainScene_NotionAR was rebuilt for Meta Quest (IP2b Mixed Reality).\n\nThe head-tracked camera is created when the scene runs.",
                "OK");
        }
    }

    [MenuItem("Tools/DECO2300/Build Quest APK")]
    public static void BuildQuestApk()
    {
        ApplyQuestPlayerSettings();

        if (!System.IO.File.Exists(ScenePath))
        {
            Debug.LogError("Missing scene: " + ScenePath);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("DECO2300", "Missing scene: " + ScenePath, "OK");
            }

            return;
        }

        string buildDir = "Builds";
        if (!System.IO.Directory.Exists(buildDir))
        {
            System.IO.Directory.CreateDirectory(buildDir);
        }

        string apkPath = buildDir + "/NotionAR_Prototype2b.apk";
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Quest APK build succeeded: " + apkPath);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("DECO2300", "Quest APK build succeeded.\n\nFile: " + apkPath, "OK");
            }
            else
            {
                EditorApplication.Exit(0);
            }
        }
        else
        {
            Debug.LogError("Quest APK build failed with " + report.summary.totalErrors + " error(s).");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("DECO2300", "Build failed with " + report.summary.totalErrors + " error(s). Check the Unity Console.", "OK");
            }
            else
            {
                EditorApplication.Exit(1);
            }
        }
    }

    public static void ValidateQuestSetup()
    {
        ApplyQuestPlayerSettings();
        List<string> errors = new List<string>();
        CheckProjectSettings(errors);

        Scene sandbox = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        try
        {
            CheckGeneratedContent(errors);
        }
        finally
        {
            if (System.IO.File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        if (errors.Count == 0)
        {
            Debug.Log("VALIDATION PASSED");
        }
        else
        {
            foreach (string error in errors)
            {
                Debug.LogError("VALIDATION: " + error);
            }
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }
        else if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog("DECO2300", "Quest setup has " + errors.Count + " problem(s). See the Console.", "OK");
        }
    }

    public static void ApplyQuestPlayerSettings()
    {
        PlayerSettings.companyName = "DECO2300";
        PlayerSettings.productName = "Notion AR";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageId);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

        EnablePassthroughOpenXRFeature(BuildTargetGroup.Android);
        AssetDatabase.SaveAssets();
    }

    private static void EnablePassthroughOpenXRFeature(BuildTargetGroup targetGroup)
    {
        try
        {
            FeatureHelpers.RefreshFeatures(targetGroup);
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
            if (settings == null)
            {
                return;
            }

            MetaQuestPassthroughFeature passthrough = settings.GetFeature<MetaQuestPassthroughFeature>();
            if (passthrough != null && !passthrough.enabled)
            {
                passthrough.enabled = true;
                EditorUtility.SetDirty(passthrough);
                EditorUtility.SetDirty(settings);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not enable MetaQuestPassthroughFeature for " + targetGroup + ": " + ex.Message);
        }
    }

    private static void CheckProjectSettings(List<string> errors)
    {
        if (PlayerSettings.colorSpace != ColorSpace.Linear)
        {
            errors.Add("Color space is not Linear.");
        }

        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
        {
            errors.Add("Android scripting backend is not IL2CPP.");
        }

        if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
        {
            errors.Add("Android target architecture is not ARM64.");
        }

        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel32)
        {
            errors.Add("Android minimum SDK is below 32.");
        }

        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != AndroidPackageId)
        {
            errors.Add("Android package id is not " + AndroidPackageId + ".");
        }

        if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.LandscapeLeft)
        {
            errors.Add("Default orientation is not Landscape Left.");
        }

        GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
        if (apis.Length == 0 || apis[0] != GraphicsDeviceType.Vulkan)
        {
            errors.Add("Android graphics API list does not start with Vulkan.");
        }

        UnityEngine.Object loader = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/XR/Loaders/OpenXRLoader.asset");
        if (loader == null)
        {
            errors.Add("OpenXR loader asset is missing.");
        }

        OpenXRSettings androidOpenXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        MetaQuestPassthroughFeature passthroughFeature = androidOpenXR != null ? androidOpenXR.GetFeature<MetaQuestPassthroughFeature>() : null;
        if (passthroughFeature == null || !passthroughFeature.enabled)
        {
            errors.Add("MetaQuestPassthroughFeature is not enabled in Android OpenXR settings.");
        }
    }

    private static void CheckGeneratedContent(List<string> errors)
    {
        GameObject host = new GameObject("Notion AR Prototype Controller");
        NotionARPrototype prototype = host.AddComponent<NotionARPrototype>();
        try
        {
            Invoke(prototype, "Start");
        }
        catch (Exception ex)
        {
            errors.Add("Start threw: " + ex.GetBaseException().Message);
            return;
        }

        bool foundOrigin = false;
        bool foundController = false;
        bool foundTable = false;
        bool foundModeToggle = false;
        bool foundGrabPrompt = false;
        Camera mainCam = null;

        List<Transform> transforms = TransformsInScene(host.scene);
        int cameraCount = 0;
        foreach (Transform transform in transforms)
        {
            Camera camera = transform.GetComponent<Camera>();
            if (camera != null)
            {
                cameraCount++;
                mainCam = camera;
                Vector3 position = camera.transform.position;
                if (position.y > 3f || position.z < -2f)
                {
                    errors.Add("Camera is still at a desktop pose: " + position);
                }

                if (camera.transform.root.name == "XR Origin")
                {
                    foundOrigin = true;
                }

                if (Mathf.Abs(camera.backgroundColor.a) > 0.01f)
                {
                    errors.Add("Camera background alpha in Passthrough mode should be 0, found " + camera.backgroundColor.a);
                }
            }

            TextMesh text = transform.GetComponent<TextMesh>();
            if (text != null && (text.text.Contains("Prototype 1") || text.text.ToLowerInvariant().Contains("desktop simulation")))
            {
                errors.Add("User-facing text still describes the desktop prototype: " + text.text);
            }

            if (text != null && text.name == "grab-prompt" && text.text == "Hold grip to grab")
            {
                foundGrabPrompt = true;
            }

            if (transform.name == "Right Controller")
            {
                foundController = true;
            }

            if (transform.name == "Mode toggle button")
            {
                foundModeToggle = true;
            }

            if (transform.name == "Tabletop / detected surface")
            {
                foundTable = true;
                if (transform.localScale.x > 2.5f || transform.localScale.z > 2f)
                {
                    errors.Add("Table is still desktop scale: " + transform.localScale);
                }

                float top = transform.position.y + transform.localScale.y * 0.5f;
                if (Mathf.Abs(top - 0.75f) > 0.02f)
                {
                    errors.Add("Table top is at " + top.ToString("0.00") + " m, not 0.75 m.");
                }
            }
        }

        if (cameraCount != 1)
        {
            errors.Add("Expected one camera, found " + cameraCount + ".");
        }

        if (!foundOrigin)
        {
            errors.Add("Head-tracked XR Origin was not created.");
        }

        if (!foundController)
        {
            errors.Add("Right Controller was not created.");
        }

        if (!foundTable)
        {
            errors.Add("Fallback virtual table was not created.");
        }

        if (!foundModeToggle)
        {
            errors.Add("Virtual table fallback mode toggle button was not created.");
        }

        if (!foundGrabPrompt)
        {
            errors.Add("Grab prompt 'Hold grip to grab' was not created.");
        }

        MethodInfo onGui = typeof(NotionARPrototype).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (onGui != null)
        {
            errors.Add("OnGUI is still defined on NotionARPrototype.");
        }

        try
        {
            // Verify fallback toggle switches virtual room visibility and camera background alpha.
            Invoke(prototype, "ToggleTableMode");
            if (mainCam != null && mainCam.backgroundColor.a < 0.99f)
            {
                errors.Add("Camera background alpha did not switch to opaque in Virtual Table fallback mode.");
            }
            Invoke(prototype, "ToggleTableMode");
            if (mainCam != null && mainCam.backgroundColor.a > 0.01f)
            {
                errors.Add("Camera background alpha did not return to 0 in Passthrough mode.");
            }

            Invoke(prototype, "OpenDocumentPicker");
            Invoke(prototype, "BeginPagePlacement");
            foreach (Transform transform in TransformsInScene(host.scene))
            {
                TextMesh text = transform.GetComponent<TextMesh>();
                if (text == null || text.name != "board-instruction")
                {
                    continue;
                }

                int lines = text.text.Split('\n').Length;
                float height = text.GetComponent<MeshRenderer>().localBounds.size.y / lines;
                float width = text.GetComponent<MeshRenderer>().localBounds.size.x;
                Debug.Log("Board instruction line height " + height.ToString("0.000") + " m, width " + width.ToString("0.000") + " m, lines " + lines + ", text: " + text.text);
                if (height < 0.02f || height > 0.08f)
                {
                    errors.Add("Instruction line height is " + height.ToString("0.000") + " m. It needs to be readable in a headset.");
                }

                if (width > 0.5f)
                {
                    errors.Add("Instruction text is wider than the side panel (" + width.ToString("0.00") + " m).");
                }
            }

            Invoke(prototype, "ConfirmPagePlacement");
            GameObject realAnchor = FindNamed(host.scene, "Real table surface anchor");
            if (realAnchor == null || !realAnchor.activeSelf)
            {
                errors.Add("ConfirmPagePlacement did not activate the real table surface anchor in Passthrough mode.");
            }

            Invoke(prototype, "RevealSketch");
            Invoke(prototype, "LiftSketchInto3D");
            GameObject house = FindNamed(host.scene, "3D object generated from the sketch");
            if (house == null || !house.activeSelf)
            {
                errors.Add("Lift did not show the 3D house.");
            }

            GameObject strokes = FindNamed(host.scene, "Sketch stroke 1");
            if (strokes == null)
            {
                errors.Add("Reveal did not create the pre-made sketch.");
            }

            Invoke(prototype, "ResetPrototype");
            if (house != null && house.activeSelf)
            {
                errors.Add("Reset left the house visible.");
            }

            if (realAnchor != null && realAnchor.activeSelf)
            {
                errors.Add("Reset left the real table anchor visible.");
            }
        }
        catch (Exception ex)
        {
            errors.Add("Prototype loop threw: " + ex.GetBaseException().Message);
        }
        finally
        {
            try
            {
                Invoke(prototype, "ClearScene");
            }
            catch (Exception ex)
            {
                errors.Add("ClearScene threw: " + ex.GetBaseException().Message);
            }
        }
    }

    private static List<Transform> TransformsInScene(Scene scene)
    {
        List<Transform> transforms = new List<Transform>();
        if (!scene.IsValid())
        {
            return transforms;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            transforms.AddRange(root.GetComponentsInChildren<Transform>(true));
        }

        return transforms;
    }

    private static GameObject FindNamed(Scene scene, string objectName)
    {
        foreach (Transform transform in TransformsInScene(scene))
        {
            if (transform.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static void Invoke(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (method == null)
        {
            throw new MissingMethodException(target.GetType().Name, methodName);
        }

        method.Invoke(target, null);
    }
}
#endif
