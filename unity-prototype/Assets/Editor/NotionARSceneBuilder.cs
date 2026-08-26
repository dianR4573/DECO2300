#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NotionARSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainScene_NotionAR.unity";

    [MenuItem("Tools/DECO2300/Rebuild Notion AR Main Scene")]
    public static void RebuildScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainScene_NotionAR";

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 4.2f, -7.2f);
        cameraObject.transform.rotation = Quaternion.Euler(58f, 0f, 0f);

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject controller = new GameObject("Notion AR Prototype Controller");
        controller.AddComponent<NotionARPrototype>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorUtility.DisplayDialog("DECO2300", "MainScene_NotionAR has been rebuilt. Press Play to test the prototype.", "OK");
    }
}
#endif
