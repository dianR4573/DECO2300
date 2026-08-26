using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime controller for the DECO2300 Notion AR horizontal prototype.
/// It builds a simple table workspace, document picker, Notion-style page,
/// 2D sketch, and 3D house object entirely from primitives so the prototype
/// can run without external assets.
/// </summary>
public class NotionARPrototype : MonoBehaviour
{
    private enum PrototypeStep
    {
        Start,
        DocumentPickerOpen,
        PagePlaced,
        SketchVisible,
        ModelLifted
    }

    private PrototypeStep step = PrototypeStep.Start;
    private GameObject table;
    private GameObject page;
    private GameObject sketchRoot;
    private GameObject houseRoot;
    private GameObject documentPicker;
    private readonly List<GameObject> generatedObjects = new List<GameObject>();
    private float liftAnimation = 0f;

    private const float HouseMoveSpeed = 2.4f;

    private void Start()
    {
        BuildScene();
        ResetPrototype();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPrototype();
            return;
        }

        if (Input.GetKeyDown(KeyCode.L) && step == PrototypeStep.Start)
        {
            OpenDocumentPicker();
        }

        if ((Input.GetKeyDown(KeyCode.Alpha1) || Input.GetMouseButtonDown(0)) && step == PrototypeStep.DocumentPickerOpen)
        {
            PlacePageOnTable();
        }

        if (Input.GetKeyDown(KeyCode.D) && step == PrototypeStep.PagePlaced)
        {
            RevealSketch();
        }

        if (Input.GetKeyDown(KeyCode.Space) && step == PrototypeStep.SketchVisible)
        {
            LiftSketchInto3D();
        }

        if (step == PrototypeStep.ModelLifted && houseRoot != null)
        {
            MoveHouseObject();
        }

        if (liftAnimation > 0f && houseRoot != null)
        {
            AnimateHouseLift();
        }
    }

    private void BuildScene()
    {
        ClearGeneratedObjects();
        EnsureCameraAndLight();
        BuildEnvironment();
        BuildDocumentPicker();
        BuildPage();
        BuildSketch();
        BuildHouseModel();
    }

    private void EnsureCameraAndLight()
    {
        if (Camera.main == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            generatedObjects.Add(cameraObject);
        }

        Camera.main.transform.position = new Vector3(0f, 4.2f, -7.2f);
        Camera.main.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        Camera.main.clearFlags = CameraClearFlags.Skybox;

        if (FindObjectOfType<Light>() == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            generatedObjects.Add(lightObject);
        }
    }

    private void BuildEnvironment()
    {
        table = CreateCube("Table / real surface anchor", new Vector3(0f, 0f, 0f), new Vector3(5.8f, 0.18f, 3.5f), new Color(0.46f, 0.30f, 0.18f));
        CreateCube("Back wall", new Vector3(0f, 1.8f, 2.05f), new Vector3(6.2f, 3.2f, 0.08f), new Color(0.86f, 0.88f, 0.91f));
        CreateCube("Floor", new Vector3(0f, -0.13f, -0.2f), new Vector3(7f, 0.06f, 5.5f), new Color(0.70f, 0.72f, 0.75f));
        CreateText("scene-label", "Notion AR workspace\nHorizontal Unity prototype", new Vector3(-2.7f, 2.6f, 1.95f), 0.22f, TextAnchor.UpperLeft, Color.black);
    }

    private void BuildDocumentPicker()
    {
        documentPicker = new GameObject("Floating Notion-style document picker");
        generatedObjects.Add(documentPicker);
        documentPicker.transform.position = new Vector3(-1.9f, 1.55f, -0.2f);

        GameObject panel = CreateCube("Picker panel", documentPicker.transform.position, new Vector3(2.5f, 1.35f, 0.06f), new Color(0.94f, 0.94f, 0.91f));
        panel.transform.SetParent(documentPicker.transform);
        CreateText("picker-title", "Select Notion page", documentPicker.transform.position + new Vector3(-1.05f, 0.46f, -0.08f), 0.16f, TextAnchor.UpperLeft, Color.black, documentPicker.transform);
        CreateText("picker-option-1", "1  House Sketch", documentPicker.transform.position + new Vector3(-1.05f, 0.15f, -0.08f), 0.14f, TextAnchor.UpperLeft, Color.black, documentPicker.transform);
        CreateText("picker-option-2", "Design Notes", documentPicker.transform.position + new Vector3(-1.05f, -0.12f, -0.08f), 0.12f, TextAnchor.UpperLeft, new Color(0.25f, 0.25f, 0.25f), documentPicker.transform);
        CreateText("picker-option-3", "Architecture Idea", documentPicker.transform.position + new Vector3(-1.05f, -0.34f, -0.08f), 0.12f, TextAnchor.UpperLeft, new Color(0.25f, 0.25f, 0.25f), documentPicker.transform);
    }

    private void BuildPage()
    {
        page = new GameObject("Notion page on table");
        generatedObjects.Add(page);
        page.transform.position = new Vector3(0f, 0.13f, -0.25f);

        GameObject pageSurface = CreateCube("Page surface", page.transform.position, new Vector3(2.7f, 0.035f, 1.75f), Color.white);
        pageSurface.transform.SetParent(page.transform);
        CreateText("page-title", "House Sketch", page.transform.position + new Vector3(-1.18f, 0.08f, 0.65f), 0.15f, TextAnchor.UpperLeft, Color.black, page.transform, true);
        CreateText("page-body", "2D idea canvas", page.transform.position + new Vector3(-1.18f, 0.08f, 0.40f), 0.10f, TextAnchor.UpperLeft, new Color(0.2f, 0.2f, 0.2f), page.transform, true);
    }

    private void BuildSketch()
    {
        sketchRoot = new GameObject("2D sketch on Notion page");
        generatedObjects.Add(sketchRoot);
        sketchRoot.transform.position = new Vector3(0f, 0.185f, -0.25f);

        CreateSketchLine("house-base", new [] { new Vector3(-0.45f, 0f, -0.15f), new Vector3(0.45f, 0f, -0.15f), new Vector3(0.45f, 0f, 0.35f), new Vector3(-0.45f, 0f, 0.35f), new Vector3(-0.45f, 0f, -0.15f) });
        CreateSketchLine("house-roof", new [] { new Vector3(-0.55f, 0f, 0.35f), new Vector3(0f, 0f, 0.75f), new Vector3(0.55f, 0f, 0.35f) });
        CreateText("sketch-label", "2D sketch", new Vector3(0.55f, 0.03f, -0.55f), 0.08f, TextAnchor.UpperLeft, Color.black, sketchRoot.transform, true);
    }

    private void BuildHouseModel()
    {
        houseRoot = new GameObject("3D spatial object generated from sketch");
        generatedObjects.Add(houseRoot);
        houseRoot.transform.position = new Vector3(0f, 0.2f, -0.25f);

        GameObject body = CreateCube("3D house body", houseRoot.transform.position + new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.7f, 0.7f), new Color(0.85f, 0.86f, 0.90f));
        body.transform.SetParent(houseRoot.transform);

        GameObject roof = CreateCube("3D roof placeholder", houseRoot.transform.position + new Vector3(0f, 0.84f, 0f), new Vector3(0.95f, 0.22f, 0.82f), new Color(0.58f, 0.12f, 0.11f));
        roof.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        roof.transform.SetParent(houseRoot.transform);

        GameObject door = CreateCube("Door", houseRoot.transform.position + new Vector3(0f, 0.15f, -0.36f), new Vector3(0.22f, 0.32f, 0.03f), new Color(0.28f, 0.16f, 0.08f));
        door.transform.SetParent(houseRoot.transform);

        CreateText("house-label", "3D object", houseRoot.transform.position + new Vector3(0.55f, 1.1f, 0f), 0.10f, TextAnchor.UpperLeft, Color.black, houseRoot.transform);
    }

    private GameObject CreateCube(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
        generatedObjects.Add(cube);
        return cube;
    }

    private void CreateSketchLine(string name, Vector3[] localPoints)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(sketchRoot.transform);
        lineObject.transform.localPosition = Vector3.zero;
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = localPoints.Length;
        line.SetPositions(localPoints);
        line.startWidth = 0.025f;
        line.endWidth = 0.025f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.black;
        line.endColor = Color.black;
    }

    private void CreateText(string name, string text, Vector3 position, float size, TextAnchor anchor, Color color, Transform parent = null, bool layFlat = false)
    {
        GameObject textObject = new GameObject(name);
        TextMesh mesh = textObject.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 64;
        mesh.characterSize = size;
        mesh.anchor = anchor;
        mesh.color = color;
        textObject.transform.position = position;
        textObject.transform.rotation = layFlat ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.Euler(0f, 0f, 0f);
        if (parent != null)
        {
            textObject.transform.SetParent(parent);
        }
        generatedObjects.Add(textObject);
    }

    private void OpenDocumentPicker()
    {
        step = PrototypeStep.DocumentPickerOpen;
        documentPicker.SetActive(true);
    }

    private void PlacePageOnTable()
    {
        step = PrototypeStep.PagePlaced;
        documentPicker.SetActive(false);
        page.SetActive(true);
    }

    private void RevealSketch()
    {
        step = PrototypeStep.SketchVisible;
        sketchRoot.SetActive(true);
    }

    private void LiftSketchInto3D()
    {
        step = PrototypeStep.ModelLifted;
        sketchRoot.SetActive(false);
        houseRoot.SetActive(true);
        houseRoot.transform.position = new Vector3(0f, 0.2f, -0.25f);
        liftAnimation = 1f;
    }

    private void MoveHouseObject()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 movement = new Vector3(horizontal, 0f, vertical) * HouseMoveSpeed * Time.deltaTime;
        houseRoot.transform.position += movement;
    }

    private void AnimateHouseLift()
    {
        liftAnimation -= Time.deltaTime;
        float targetY = Mathf.Lerp(houseRoot.transform.position.y, 0.95f, 4f * Time.deltaTime);
        houseRoot.transform.position = new Vector3(houseRoot.transform.position.x, targetY, houseRoot.transform.position.z);
    }

    private void ResetPrototype()
    {
        step = PrototypeStep.Start;
        liftAnimation = 0f;
        if (documentPicker != null) documentPicker.SetActive(false);
        if (page != null) page.SetActive(false);
        if (sketchRoot != null) sketchRoot.SetActive(false);
        if (houseRoot != null)
        {
            houseRoot.SetActive(false);
            houseRoot.transform.position = new Vector3(0f, 0.2f, -0.25f);
        }
    }

    private void ClearGeneratedObjects()
    {
        foreach (GameObject generated in generatedObjects)
        {
            if (generated != null)
            {
                Destroy(generated);
            }
        }
        generatedObjects.Clear();
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(16, 16, 520, 148), "Notion AR — Interactive Prototype 1");
        GUI.Label(new Rect(32, 46, 500, 24), GetInstructionText());
        GUI.Label(new Rect(32, 74, 500, 24), "Controls: L = picker, 1/click = select page, D = sketch, Space = lift, WASD/arrows = move, R = reset");
        GUI.Label(new Rect(32, 102, 500, 44), "Testing focus: Does this table-based 2D-to-3D Notion workflow feel understandable and useful?");
    }

    private string GetInstructionText()
    {
        switch (step)
        {
            case PrototypeStep.Start:
                return "Step 1: Press L to simulate the L-shaped hand gesture and open Notion pages.";
            case PrototypeStep.DocumentPickerOpen:
                return "Step 2: Press 1 or click to choose the House Sketch page.";
            case PrototypeStep.PagePlaced:
                return "Step 3: The page is on the table. Press D to reveal the 2D sketch.";
            case PrototypeStep.SketchVisible:
                return "Step 4: Press Space to simulate lifting the 2D sketch into 3D.";
            case PrototypeStep.ModelLifted:
                return "Step 5: Move the 3D object with WASD or arrow keys. Ask the tester post-test questions.";
            default:
                return string.Empty;
        }
    }
}
