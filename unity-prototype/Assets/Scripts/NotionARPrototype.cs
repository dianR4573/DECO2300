using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Desktop horizontal prototype for the Notion AR concept.
///
/// The scene deliberately uses only Unity primitives so it remains portable, but
/// the interaction sequence mirrors the proposed Quest experience: open a page
/// picker, place a page on a detected surface, draw on it, lift the drawing into
/// 3D, then pinch-drag the result through space.
/// </summary>
public sealed class NotionARPrototype : MonoBehaviour
{
    private enum PrototypeStep
    {
        Welcome,
        DocumentPicker,
        PlacingPage,
        PagePlaced,
        Drawing,
        ModelLifted
    }

    private const float TableTopY = 0.12f;
    private const float PageY = 0.23f;
    private const float HouseMoveSpeed = 2.1f;
    private const float LiftDuration = 1.15f;
    private const float MinimumStrokeSpacing = 0.025f;
    private const float WorldTextScale = 0.24f;
    private const float PageHalfWidth = 1.425f;
    private const float PageHalfDepth = 0.925f;
    private const float DrawingEdgeMargin = 0.025f;

    private readonly List<GameObject> sceneRoots = new List<GameObject>();
    private readonly List<GameObject> userStrokes = new List<GameObject>();
    private readonly List<Material> runtimeMaterials = new List<Material>();

    private PrototypeStep step;
    private GameObject documentPicker;
    private GameObject roomBranding;
    private GameObject page;
    private GameObject sketchGuide;
    private GameObject drawingRoot;
    private GameObject houseRoot;
    private GameObject activeStroke;
    private LineRenderer activeStrokeRenderer;
    private Renderer pageSurfaceRenderer;
    private Collider pageSurfaceCollider;
    private LineRenderer placementOutline;
    private TextMesh pagePromptText;
    private Material pageMaterial;
    private Material drawingMaterial;

    private bool hasDrawing;
    private bool pointerDraggingHouse;
    private Vector3 dragOffset;
    private Vector3 lastStrokePoint;
    private int activeStrokePointCount;
    private float liftElapsed;
    private Vector3 liftStartPosition;

    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle eyebrowStyle;
    private GUIStyle bodyStyle;
    private GUIStyle mutedStyle;
    private GUIStyle stepStyle;
    private GUIStyle activeStepStyle;
    private GUIStyle primaryButtonStyle;
    private GUIStyle secondaryButtonStyle;
    private Texture2D panelTexture;
    private Texture2D activeStepTexture;
    private Texture2D primaryTexture;
    private Texture2D secondaryTexture;

    private static readonly Color InkColor = new Color(0.20f, 0.36f, 0.95f);
    private static readonly Color NotionInk = new Color(0.12f, 0.13f, 0.15f);
    private static readonly Color SuccessGreen = new Color(0.22f, 0.70f, 0.50f);

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

        HandleStepShortcuts();

        if (step == PrototypeStep.PlacingPage)
        {
            UpdatePagePlacement();
        }
        else if (step == PrototypeStep.DocumentPicker)
        {
            UpdateDocumentPicker();
        }
        else if (step == PrototypeStep.Drawing)
        {
            UpdateDrawingInput();
        }
        else if (step == PrototypeStep.ModelLifted)
        {
            UpdateHouseInteraction();
        }

        if (liftElapsed > 0f)
        {
            AnimateHouseLift();
        }
    }

    private void HandleStepShortcuts()
    {
        if (step == PrototypeStep.Welcome && Input.GetKeyDown(KeyCode.L))
        {
            OpenDocumentPicker();
        }
        else if (step == PrototypeStep.DocumentPicker && Input.GetKeyDown(KeyCode.Alpha1))
        {
            BeginPagePlacement();
        }
        else if (step == PrototypeStep.PlacingPage && Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmPagePlacement();
        }
        else if (step == PrototypeStep.PagePlaced && Input.GetKeyDown(KeyCode.D))
        {
            RevealSketch();
        }
        else if (step == PrototypeStep.Drawing && hasDrawing && Input.GetKeyDown(KeyCode.Space))
        {
            LiftSketchInto3D();
        }
    }

    private void BuildScene()
    {
        ClearScene();
        EnsureCameraAndLight();
        BuildEnvironment();
        BuildDocumentPicker();
        BuildPage();
        BuildHouseModel();
    }

    private void EnsureCameraAndLight()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            sceneRoots.Add(cameraObject);
        }

        camera.transform.position = new Vector3(0f, 4.35f, -7.35f);
        camera.transform.rotation = Quaternion.Euler(29f, 0f, 0f);
        camera.fieldOfView = 48f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.74f, 0.79f, 0.82f);

        if (FindFirstObjectByType<Light>() == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            sceneRoots.Add(lightObject);
        }
    }

    private void BuildEnvironment()
    {
        GameObject environment = NewRoot("Mixed-reality room simulation");

        CreateCube("Tabletop / detected surface", environment.transform, Vector3.zero, new Vector3(6.2f, 0.20f, 3.7f), new Color(0.39f, 0.25f, 0.15f));
        CreateCube("Table front", environment.transform, new Vector3(0f, -0.72f, 1.55f), new Vector3(5.7f, 1.35f, 0.16f), new Color(0.28f, 0.17f, 0.10f));
        CreateCube("Floor", environment.transform, new Vector3(0f, -0.18f, 1.2f), new Vector3(10f, 0.06f, 8f), new Color(0.64f, 0.67f, 0.68f));
        CreateCube("Back wall", environment.transform, new Vector3(0f, 2.35f, 3.0f), new Vector3(10f, 5f, 0.08f), new Color(0.83f, 0.85f, 0.84f));

        CreateCube("Surface scan line left", environment.transform, new Vector3(-2.95f, 0.115f, -1.72f), new Vector3(0.7f, 0.012f, 0.025f), SuccessGreen);
        CreateCube("Surface scan line right", environment.transform, new Vector3(2.95f, 0.115f, -1.72f), new Vector3(0.7f, 0.012f, 0.025f), SuccessGreen);
        CreateText("surface-status", "SURFACE FOUND  /  TABLE", environment.transform, new Vector3(-2.55f, 0.135f, -1.64f), 0.055f, TextAnchor.UpperLeft, SuccessGreen, true);

        GameObject plantPot = CreateCylinder("Desk plant pot", environment.transform, new Vector3(2.5f, 0.34f, 0.9f), new Vector3(0.35f, 0.28f, 0.35f), new Color(0.62f, 0.44f, 0.28f));
        CreateSphere("Plant leaf 1", plantPot.transform, new Vector3(0f, 0.52f, 0f), new Vector3(0.20f, 0.55f, 0.16f), new Color(0.22f, 0.45f, 0.27f));
        CreateSphere("Plant leaf 2", plantPot.transform, new Vector3(-0.18f, 0.42f, 0f), new Vector3(0.18f, 0.42f, 0.14f), new Color(0.28f, 0.54f, 0.31f));
        CreateSphere("Plant leaf 3", plantPot.transform, new Vector3(0.18f, 0.40f, 0.04f), new Vector3(0.17f, 0.40f, 0.14f), new Color(0.19f, 0.40f, 0.24f));

        roomBranding = new GameObject("Room branding");
        roomBranding.transform.SetParent(environment.transform, false);
        CreateText("scene-title", "NOTION  /  SPATIAL CANVAS", roomBranding.transform, new Vector3(-2.75f, 2.6f, 2.92f), 0.13f, TextAnchor.UpperLeft, NotionInk, false);
        CreateText("scene-subtitle", "Desktop simulation of a Meta Quest 3 workflow", roomBranding.transform, new Vector3(-2.75f, 2.33f, 2.92f), 0.07f, TextAnchor.UpperLeft, new Color(0.30f, 0.32f, 0.34f), false);
    }

    private void BuildDocumentPicker()
    {
        documentPicker = NewRoot("Floating Notion page picker");
        documentPicker.transform.position = new Vector3(0.55f, 1.50f, 0.55f);

        CreateCube("Picker backdrop", documentPicker.transform, Vector3.zero, new Vector3(3.15f, 1.85f, 0.07f), new Color(0.96f, 0.96f, 0.94f));
        CreateText("picker-eyebrow", "NOTION  /  RECENT PAGES", documentPicker.transform, new Vector3(-1.36f, 0.72f, -0.05f), 0.065f, TextAnchor.UpperLeft, new Color(0.38f, 0.40f, 0.42f), false);
        CreateText("picker-title", "Choose a canvas", documentPicker.transform, new Vector3(-1.36f, 0.48f, -0.05f), 0.13f, TextAnchor.UpperLeft, NotionInk, false);

        CreatePickerCard("1", "House Sketch", "Edited today  /  2D design", -0.01f, true);
        CreatePickerCard("2", "Studio Notes", "Yesterday  /  text page", -0.39f, false);
        CreatePickerCard("3", "Material Study", "Monday  /  gallery", -0.71f, false);
    }

    private void CreatePickerCard(string number, string title, string detail, float localY, bool selected)
    {
        Color cardColor = selected ? new Color(0.87f, 0.91f, 1f) : new Color(0.89f, 0.89f, 0.87f);
        CreateCube("Page option " + number + " - " + title, documentPicker.transform, new Vector3(0f, localY, -0.06f), new Vector3(2.75f, 0.28f, 0.05f), cardColor);
        CreateText("option-number-" + number, number, documentPicker.transform, new Vector3(-1.25f, localY + 0.04f, -0.10f), 0.07f, TextAnchor.UpperLeft, selected ? InkColor : NotionInk, false);
        CreateText("option-title-" + number, title, documentPicker.transform, new Vector3(-1.02f, localY + 0.04f, -0.10f), 0.075f, TextAnchor.UpperLeft, NotionInk, false);
        CreateText("option-detail-" + number, detail, documentPicker.transform, new Vector3(0.05f, localY + 0.025f, -0.10f), 0.048f, TextAnchor.UpperLeft, new Color(0.35f, 0.37f, 0.39f), false);
    }

    private void BuildPage()
    {
        page = NewRoot("Notion page canvas");
        page.transform.position = new Vector3(0f, PageY, -0.25f);

        pageMaterial = CreateMaterial(new Color(0.98f, 0.98f, 0.97f));
        ConfigureTransparentMaterial(pageMaterial);
        GameObject pageSurface = CreateCube("Page surface", page.transform, Vector3.zero, new Vector3(2.85f, 0.045f, 1.85f), Color.white, pageMaterial);
        pageSurfaceRenderer = pageSurface.GetComponent<Renderer>();
        pageSurfaceCollider = pageSurface.GetComponent<Collider>();

        CreateText("page-kicker", "NOTION  /  DESIGN STUDY", page.transform, new Vector3(-1.20f, 0.045f, 0.70f), 0.045f, TextAnchor.UpperLeft, new Color(0.40f, 0.42f, 0.44f), true);
        CreateText("page-title", "House Sketch", page.transform, new Vector3(-1.20f, 0.048f, 0.46f), 0.11f, TextAnchor.UpperLeft, NotionInk, true);
        pagePromptText = CreateText("page-prompt", "Spatial canvas ready", page.transform, new Vector3(-1.20f, 0.048f, 0.22f), 0.047f, TextAnchor.UpperLeft, new Color(0.36f, 0.38f, 0.41f), true);

        sketchGuide = new GameObject("Faint 2D house guide");
        sketchGuide.transform.SetParent(page.transform, false);
        CreateFlatLine("guide-house-outline", sketchGuide.transform, new[]
        {
            new Vector3(-0.52f, 0.052f, -0.52f), new Vector3(0.52f, 0.052f, -0.52f),
            new Vector3(0.52f, 0.052f, 0.05f), new Vector3(0f, 0.052f, 0.49f),
            new Vector3(-0.52f, 0.052f, 0.05f),
            new Vector3(-0.52f, 0.052f, -0.52f)
        }, 0.018f, new Color(0.68f, 0.70f, 0.72f, 0.72f));

        drawingRoot = new GameObject("User drawing strokes");
        drawingRoot.transform.SetParent(page.transform, false);
        drawingMaterial = CreateMaterial(InkColor, "Sprites/Default");

        GameObject outlineObject = new GameObject("Placement boundary");
        outlineObject.transform.SetParent(page.transform, false);
        placementOutline = outlineObject.AddComponent<LineRenderer>();
        placementOutline.useWorldSpace = false;
        placementOutline.loop = true;
        placementOutline.positionCount = 4;
        placementOutline.SetPositions(new[]
        {
            new Vector3(-1.49f, 0.07f, -0.99f), new Vector3(1.49f, 0.07f, -0.99f),
            new Vector3(1.49f, 0.07f, 0.99f), new Vector3(-1.49f, 0.07f, 0.99f)
        });
        placementOutline.startWidth = 0.025f;
        placementOutline.endWidth = 0.025f;
        placementOutline.sharedMaterial = CreateMaterial(SuccessGreen, "Sprites/Default");
        placementOutline.startColor = SuccessGreen;
        placementOutline.endColor = SuccessGreen;
    }

    private void BuildHouseModel()
    {
        houseRoot = NewRoot("3D object generated from the sketch");
        houseRoot.transform.position = new Vector3(0f, PageY, -0.25f);

        GameObject body = CreateCube("House body", houseRoot.transform, new Vector3(0f, 0.38f, 0f), new Vector3(1.10f, 0.76f, 0.86f), new Color(0.91f, 0.88f, 0.80f));
        body.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        CreateGableRoof(houseRoot.transform);
        CreateCube("Front door", houseRoot.transform, new Vector3(0f, 0.27f, -0.445f), new Vector3(0.25f, 0.50f, 0.035f), new Color(0.32f, 0.22f, 0.15f));
        CreateCube("Left window", houseRoot.transform, new Vector3(-0.34f, 0.46f, -0.448f), new Vector3(0.22f, 0.22f, 0.03f), new Color(0.34f, 0.63f, 0.76f));
        CreateCube("Right window", houseRoot.transform, new Vector3(0.34f, 0.46f, -0.448f), new Vector3(0.22f, 0.22f, 0.03f), new Color(0.34f, 0.63f, 0.76f));
        CreateCube("Chimney", houseRoot.transform, new Vector3(0.32f, 1.06f, 0.15f), new Vector3(0.16f, 0.40f, 0.18f), new Color(0.52f, 0.22f, 0.16f));

        GameObject selectionRing = new GameObject("Pinch selection ring");
        selectionRing.transform.SetParent(houseRoot.transform, false);
        LineRenderer ring = selectionRing.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 48;
        Vector3[] ringPoints = new Vector3[48];
        for (int i = 0; i < ringPoints.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / ringPoints.Length;
            ringPoints[i] = new Vector3(Mathf.Cos(angle) * 0.78f, 0.02f, Mathf.Sin(angle) * 0.66f);
        }
        ring.SetPositions(ringPoints);
        ring.startWidth = 0.018f;
        ring.endWidth = 0.018f;
        ring.sharedMaterial = CreateMaterial(new Color(0.26f, 0.53f, 1f, 0.75f), "Sprites/Default");

        CreateText("house-label", "PINCH TO MOVE", houseRoot.transform, new Vector3(-0.46f, 1.35f, 0f), 0.055f, TextAnchor.UpperLeft, InkColor, false);
    }

    private void CreateGableRoof(Transform parent)
    {
        GameObject roofObject = new GameObject("Gable roof");
        roofObject.transform.SetParent(parent, false);
        roofObject.transform.localPosition = new Vector3(0f, 0.76f, 0f);

        const float halfWidth = 0.68f;
        const float halfDepth = 0.52f;
        const float height = 0.48f;
        Mesh mesh = new Mesh { name = "Generated gable roof mesh" };
        mesh.vertices = new[]
        {
            new Vector3(-halfWidth, 0f, -halfDepth), new Vector3(halfWidth, 0f, -halfDepth), new Vector3(0f, height, -halfDepth),
            new Vector3(-halfWidth, 0f, halfDepth), new Vector3(halfWidth, 0f, halfDepth), new Vector3(0f, height, halfDepth)
        };
        mesh.triangles = new[]
        {
            0, 2, 1, 3, 4, 5,
            0, 3, 5, 0, 5, 2,
            1, 2, 5, 1, 5, 4,
            0, 1, 4, 0, 4, 3
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        roofObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        roofObject.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial(new Color(0.58f, 0.16f, 0.13f));
        roofObject.AddComponent<BoxCollider>().size = new Vector3(halfWidth * 2f, height, halfDepth * 2f);
    }

    private void OpenDocumentPicker()
    {
        step = PrototypeStep.DocumentPicker;
        if (roomBranding != null) roomBranding.SetActive(false);
        documentPicker.SetActive(true);
    }

    private void UpdateDocumentPicker()
    {
        if (!Input.GetMouseButtonDown(0) || IsPointerOverHud())
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 30f) && hit.transform.name.StartsWith("Page option 1"))
        {
            BeginPagePlacement();
        }
    }

    private void BeginPagePlacement()
    {
        step = PrototypeStep.PlacingPage;
        documentPicker.SetActive(false);
        page.SetActive(true);
        sketchGuide.SetActive(false);
        drawingRoot.SetActive(false);
        placementOutline.gameObject.SetActive(true);
        SetPageAlpha(0.78f);
        SetPagePrompt("Move with mouse, then click to place");
    }

    private void UpdatePagePlacement()
    {
        Vector3 surfacePoint;
        if (TryGetPointerOnHorizontalPlane(TableTopY, out surfacePoint) && !IsPointerOverHud())
        {
            page.transform.position = new Vector3(
                Mathf.Clamp(surfacePoint.x, -1.55f, 1.55f),
                PageY,
                Mathf.Clamp(surfacePoint.z, -0.75f, 0.72f));
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverHud())
        {
            ConfirmPagePlacement();
        }
    }

    private void ConfirmPagePlacement()
    {
        step = PrototypeStep.PagePlaced;
        placementOutline.gameObject.SetActive(false);
        sketchGuide.SetActive(false);
        drawingRoot.SetActive(false);
        SetPageAlpha(1f);
        SetPagePrompt("Canvas anchored  /  Press D to reveal sketch");
    }

    private void RevealSketch()
    {
        step = PrototypeStep.Drawing;
        sketchGuide.SetActive(true);
        drawingRoot.SetActive(true);
        AddSampleHouseSketch();
        SetPagePrompt("2D sketch ready  /  Space lifts it into 3D");
    }

    private void UpdateDrawingInput()
    {
        if (Input.GetMouseButtonDown(0) && !IsPointerOverHud())
        {
            Vector3 pagePoint;
            if (TryGetPointOnPage(out pagePoint))
            {
                BeginStroke(pagePoint);
            }
        }

        if (Input.GetMouseButton(0) && activeStrokeRenderer != null)
        {
            Vector3 pagePoint;
            if (TryGetPointOnPage(out pagePoint) && Vector3.Distance(lastStrokePoint, pagePoint) >= MinimumStrokeSpacing)
            {
                AddPointToActiveStroke(pagePoint);
            }
        }

        if (Input.GetMouseButtonUp(0) && activeStrokeRenderer != null)
        {
            EndStroke();
        }
    }

    private void BeginStroke(Vector3 worldPoint)
    {
        activeStroke = new GameObject("User stroke " + (userStrokes.Count + 1));
        activeStroke.transform.SetParent(drawingRoot.transform, true);
        activeStrokeRenderer = activeStroke.AddComponent<LineRenderer>();
        activeStrokeRenderer.useWorldSpace = true;
        activeStrokeRenderer.positionCount = 1;
        activeStrokeRenderer.SetPosition(0, worldPoint);
        activeStrokeRenderer.startWidth = 0.035f;
        activeStrokeRenderer.endWidth = 0.035f;
        activeStrokeRenderer.numCapVertices = 5;
        activeStrokeRenderer.numCornerVertices = 5;
        activeStrokeRenderer.sharedMaterial = drawingMaterial;
        activeStrokeRenderer.startColor = InkColor;
        activeStrokeRenderer.endColor = InkColor;
        activeStrokePointCount = 1;
        lastStrokePoint = worldPoint;
    }

    private void AddPointToActiveStroke(Vector3 worldPoint)
    {
        activeStrokePointCount++;
        activeStrokeRenderer.positionCount = activeStrokePointCount;
        activeStrokeRenderer.SetPosition(activeStrokePointCount - 1, worldPoint);
        lastStrokePoint = worldPoint;
    }

    private void EndStroke()
    {
        if (activeStrokePointCount >= 2)
        {
            userStrokes.Add(activeStroke);
            hasDrawing = true;
        }
        else
        {
            Destroy(activeStroke);
        }

        activeStroke = null;
        activeStrokeRenderer = null;
        activeStrokePointCount = 0;
    }

    private void AddSampleHouseSketch()
    {
        if (hasDrawing)
        {
            return;
        }

        Vector3 origin = page.transform.position + new Vector3(0f, 0.065f, -0.20f);
        AddSampleStroke(new[]
        {
            origin + new Vector3(-0.52f, 0f, -0.30f), origin + new Vector3(0.52f, 0f, -0.30f),
            origin + new Vector3(0.52f, 0f, 0.26f), origin + new Vector3(0f, 0f, 0.70f),
            origin + new Vector3(-0.52f, 0f, 0.26f),
            origin + new Vector3(-0.52f, 0f, -0.30f)
        });
        AddSampleStroke(new[]
        {
            origin + new Vector3(-0.12f, 0f, -0.30f), origin + new Vector3(-0.12f, 0f, 0.02f),
            origin + new Vector3(0.12f, 0f, 0.02f), origin + new Vector3(0.12f, 0f, -0.30f)
        });
        hasDrawing = true;
    }

    private void AddSampleStroke(Vector3[] points)
    {
        GameObject stroke = new GameObject("Sample user stroke " + (userStrokes.Count + 1));
        stroke.transform.SetParent(drawingRoot.transform, true);
        LineRenderer line = stroke.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.startWidth = 0.035f;
        line.endWidth = 0.035f;
        line.numCapVertices = 5;
        line.numCornerVertices = 5;
        line.sharedMaterial = drawingMaterial;
        line.startColor = InkColor;
        line.endColor = InkColor;
        userStrokes.Add(stroke);
    }

    private void LiftSketchInto3D()
    {
        if (!hasDrawing)
        {
            return;
        }

        step = PrototypeStep.ModelLifted;
        sketchGuide.SetActive(false);
        houseRoot.SetActive(true);
        liftStartPosition = page.transform.position + new Vector3(0f, 0.01f, 0f);
        houseRoot.transform.position = liftStartPosition;
        houseRoot.transform.rotation = Quaternion.identity;
        houseRoot.transform.localScale = Vector3.one * 0.12f;
        liftElapsed = 0.001f;
    }

    private void AnimateHouseLift()
    {
        liftElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(liftElapsed / LiftDuration);
        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        float flourish = Mathf.Sin(progress * Mathf.PI) * 0.12f;

        houseRoot.transform.position = Vector3.Lerp(liftStartPosition, liftStartPosition + new Vector3(0f, 1.03f, 0f), eased) + Vector3.up * flourish;
        houseRoot.transform.localScale = Vector3.Lerp(Vector3.one * 0.12f, Vector3.one, eased);
        houseRoot.transform.rotation = Quaternion.Euler(0f, Mathf.Sin(progress * Mathf.PI) * 12f, 0f);

        if (progress > 0.42f)
        {
            drawingRoot.SetActive(false);
        }

        if (progress >= 1f)
        {
            liftElapsed = 0f;
        }
    }

    private void UpdateHouseInteraction()
    {
        if (liftElapsed > 0f)
        {
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 movement = new Vector3(horizontal, 0f, vertical) * HouseMoveSpeed * Time.deltaTime;
        Vector3 movedPosition = houseRoot.transform.position + movement;
        houseRoot.transform.position = new Vector3(
            Mathf.Clamp(movedPosition.x, -2.4f, 2.4f),
            movedPosition.y,
            Mathf.Clamp(movedPosition.z, -1.1f, 1.35f));

        float rotation = 0f;
        if (Input.GetKey(KeyCode.Q)) rotation += 65f * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) rotation -= 65f * Time.deltaTime;
        houseRoot.transform.Rotate(Vector3.up, rotation, Space.World);

        if (Input.mouseScrollDelta.y != 0f && !IsPointerOverHud())
        {
            float scale = Mathf.Clamp(houseRoot.transform.localScale.x + Input.mouseScrollDelta.y * 0.08f, 0.65f, 1.55f);
            houseRoot.transform.localScale = Vector3.one * scale;
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverHud() && PointerHitsHouse())
        {
            Vector3 point;
            if (TryGetPointerOnHorizontalPlane(houseRoot.transform.position.y, out point))
            {
                pointerDraggingHouse = true;
                dragOffset = houseRoot.transform.position - point;
            }
        }

        if (pointerDraggingHouse && Input.GetMouseButton(0))
        {
            Vector3 point;
            if (TryGetPointerOnHorizontalPlane(houseRoot.transform.position.y, out point))
            {
                Vector3 target = point + dragOffset;
                houseRoot.transform.position = new Vector3(
                    Mathf.Clamp(target.x, -2.4f, 2.4f),
                    houseRoot.transform.position.y,
                    Mathf.Clamp(target.z, -1.1f, 1.35f));
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            pointerDraggingHouse = false;
        }
    }

    private bool PointerHitsHouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        return Physics.Raycast(ray, out hit, 30f) && (hit.transform == houseRoot.transform || hit.transform.IsChildOf(houseRoot.transform));
    }

    private bool TryGetPointOnPage(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (pageSurfaceCollider == null || Camera.main == null)
        {
            return false;
        }

        Ray pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit pageHit;
        if (!pageSurfaceCollider.Raycast(pointerRay, out pageHit, 30f))
        {
            return false;
        }

        Vector3 localPoint = page.transform.InverseTransformPoint(pageHit.point);
        bool inside = Mathf.Abs(localPoint.x) <= PageHalfWidth - DrawingEdgeMargin
            && Mathf.Abs(localPoint.z) <= PageHalfDepth - DrawingEdgeMargin;

        // Keep the ink visibly attached to the paper while avoiding z-fighting
        // with the page surface and its printed guide.
        worldPoint = pageHit.point + page.transform.up * 0.035f;
        return inside;
    }

    private bool TryGetPointerOnHorizontalPlane(float height, out Vector3 worldPoint)
    {
        Plane plane = new Plane(Vector3.up, new Vector3(0f, height, 0f));
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float distance;
        if (plane.Raycast(ray, out distance))
        {
            worldPoint = ray.GetPoint(distance);
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }

    private bool IsPointerOverHud()
    {
        float topOriginY = Screen.height - Input.mousePosition.y;
        return (Input.mousePosition.x <= 390f && topOriginY <= 455f) || topOriginY >= Screen.height - 92f;
    }

    private void SetPageAlpha(float alpha)
    {
        if (pageSurfaceRenderer == null)
        {
            return;
        }

        Color color = pageSurfaceRenderer.sharedMaterial.color;
        color.a = alpha;
        pageSurfaceRenderer.sharedMaterial.color = color;
    }

    private void SetPagePrompt(string text)
    {
        if (pagePromptText != null)
        {
            pagePromptText.text = text;
        }
    }

    private void ResetPrototype()
    {
        step = PrototypeStep.Welcome;
        hasDrawing = false;
        pointerDraggingHouse = false;
        liftElapsed = 0f;

        if (activeStroke != null)
        {
            Destroy(activeStroke);
        }
        activeStroke = null;
        activeStrokeRenderer = null;

        foreach (GameObject stroke in userStrokes)
        {
            if (stroke != null)
            {
                Destroy(stroke);
            }
        }
        userStrokes.Clear();

        if (documentPicker != null) documentPicker.SetActive(false);
        if (roomBranding != null) roomBranding.SetActive(true);
        if (page != null)
        {
            page.SetActive(false);
            page.transform.position = new Vector3(0f, PageY, -0.25f);
        }
        if (sketchGuide != null) sketchGuide.SetActive(false);
        if (drawingRoot != null) drawingRoot.SetActive(false);
        if (placementOutline != null) placementOutline.gameObject.SetActive(false);
        if (houseRoot != null)
        {
            houseRoot.SetActive(false);
            houseRoot.transform.position = new Vector3(0f, PageY, -0.25f);
            houseRoot.transform.rotation = Quaternion.identity;
            houseRoot.transform.localScale = Vector3.one;
        }
        SetPageAlpha(1f);
        SetPagePrompt("Spatial canvas ready");
    }

    private GameObject NewRoot(string name)
    {
        GameObject root = new GameObject(name);
        sceneRoots.Add(root);
        return root;
    }

    private GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 scale, Color color, Material material = null)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material != null ? material : CreateMaterial(color);
        return cube;
    }

    private GameObject CreateSphere(string name, Transform parent, Vector3 localPosition, Vector3 scale, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = localPosition;
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color);
        return sphere;
    }

    private GameObject CreateCylinder(string name, Transform parent, Vector3 localPosition, Vector3 scale, Color color)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color);
        return cylinder;
    }

    private void CreateFlatLine(string name, Transform parent, Vector3[] localPoints, float width, Color color)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = localPoints.Length;
        line.SetPositions(localPoints);
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 3;
        line.numCornerVertices = 3;
        line.sharedMaterial = CreateMaterial(color, "Sprites/Default");
        line.startColor = color;
        line.endColor = color;
    }

    private TextMesh CreateText(string name, string text, Transform parent, Vector3 localPosition, float size, TextAnchor anchor, Color color, bool layFlat)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = layFlat ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
        TextMesh mesh = textObject.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 72;
        // TextMesh font size affects its physical world footprint. Keeping a
        // single scale correction here prevents every spatial label from
        // overflowing its panel while preserving a crisp 72-point font.
        mesh.characterSize = size * WorldTextScale;
        mesh.anchor = anchor;
        mesh.color = color;
        return mesh;
    }

    private Material CreateMaterial(Color color, string preferredShader = "Standard")
    {
        Shader shader = Shader.Find(preferredShader);
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        Material material = new Material(shader) { color = color };
        runtimeMaterials.Add(material);
        return material;
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }

    private void ClearScene()
    {
        foreach (GameObject root in sceneRoots)
        {
            if (root != null)
            {
                Destroy(root);
            }
        }
        sceneRoots.Clear();

        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
        runtimeMaterials.Clear();
    }

    private void OnDestroy()
    {
        ClearScene();
        if (panelTexture != null) Destroy(panelTexture);
        if (activeStepTexture != null) Destroy(activeStepTexture);
        if (primaryTexture != null) Destroy(primaryTexture);
        if (secondaryTexture != null) Destroy(secondaryTexture);
    }

    private void EnsureGuiStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelTexture = MakeTexture(new Color(0.965f, 0.96f, 0.94f, 0.97f));
        activeStepTexture = MakeTexture(new Color(0.87f, 0.91f, 1f, 1f));
        primaryTexture = MakeTexture(InkColor);
        secondaryTexture = MakeTexture(new Color(0.88f, 0.87f, 0.84f));

        panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = panelTexture }, padding = new RectOffset(22, 22, 20, 20) };
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold, normal = { textColor = NotionInk } };
        eyebrowStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = InkColor } };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = NotionInk } };
        mutedStyle = new GUIStyle(bodyStyle) { fontSize = 12, normal = { textColor = new Color(0.34f, 0.36f, 0.39f) } };
        stepStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, padding = new RectOffset(10, 8, 5, 5), normal = { textColor = new Color(0.42f, 0.43f, 0.45f) } };
        activeStepStyle = new GUIStyle(stepStyle) { fontStyle = FontStyle.Bold, normal = { textColor = NotionInk, background = activeStepTexture } };
        primaryButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, fixedHeight = 38f, normal = { textColor = Color.white, background = primaryTexture } };
        secondaryButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fixedHeight = 34f, normal = { textColor = NotionInk, background = secondaryTexture } };
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        GUILayout.BeginArea(new Rect(20f, 20f, 350f, 425f), panelStyle);
        GUILayout.Label("INTERACTIVE PROTOTYPE 1", eyebrowStyle);
        GUILayout.Space(2f);
        GUILayout.Label("Notion AR", titleStyle);
        GUILayout.Label("Turn a flat note into a spatial idea.", mutedStyle);
        GUILayout.Space(14f);

        DrawProgress();
        GUILayout.Space(12f);
        GUILayout.Label(GetInstructionText(), bodyStyle, GUILayout.Height(58f));
        DrawCurrentAction();
        GUILayout.FlexibleSpace();
        GUILayout.Label("R  Reset at any time", mutedStyle);
        GUILayout.EndArea();

        string mode = step == PrototypeStep.ModelLifted ? "PINCH MODE" : (step == PrototypeStep.Drawing ? "INK MODE" : "GUIDED MODE");
        GUI.Box(new Rect(Screen.width - 150f, 20f, 130f, 30f), mode);

        GUILayout.BeginArea(new Rect(20f, Screen.height - 72f, Screen.width - 40f, 52f), panelStyle);
        GUILayout.Label(GetControlHint(), mutedStyle);
        GUILayout.EndArea();
    }

    private void DrawProgress()
    {
        string[] labels = { "1  Select", "2  Place", "3  Sketch", "4  Lift", "5  Explore" };
        int activeIndex = GetProgressIndex();
        GUILayout.BeginHorizontal();
        for (int i = 0; i < labels.Length; i++)
        {
            GUILayout.Label(labels[i], i == activeIndex ? activeStepStyle : stepStyle, GUILayout.ExpandWidth(true));
        }
        GUILayout.EndHorizontal();
    }

    private int GetProgressIndex()
    {
        switch (step)
        {
            case PrototypeStep.Welcome:
            case PrototypeStep.DocumentPicker: return 0;
            case PrototypeStep.PlacingPage: return 1;
            case PrototypeStep.PagePlaced: return 2;
            case PrototypeStep.Drawing: return hasDrawing ? 3 : 2;
            case PrototypeStep.ModelLifted: return liftElapsed > 0f ? 3 : 4;
            default: return 0;
        }
    }

    private void DrawCurrentAction()
    {
        if (step == PrototypeStep.Welcome)
        {
            if (GUILayout.Button("Make L gesture  [L]", primaryButtonStyle)) OpenDocumentPicker();
        }
        else if (step == PrototypeStep.DocumentPicker)
        {
            if (GUILayout.Button("Open House Sketch  [1]", primaryButtonStyle)) BeginPagePlacement();
            GUILayout.Space(5f);
            GUILayout.Label("Other pages are shown to make selection feel contextual; this prototype follows one test scenario.", mutedStyle);
        }
        else if (step == PrototypeStep.PlacingPage)
        {
            if (GUILayout.Button("Place on detected table  [Enter]", primaryButtonStyle)) ConfirmPagePlacement();
        }
        else if (step == PrototypeStep.PagePlaced)
        {
            if (GUILayout.Button("Reveal 2D sketch  [D]", primaryButtonStyle)) RevealSketch();
        }
        else if (step == PrototypeStep.Drawing)
        {
            if (GUILayout.Button("Lift sketch into 3D  [Space]", primaryButtonStyle))
            {
                LiftSketchInto3D();
            }
            GUILayout.Space(5f);
            GUILayout.Label("Optional: click and drag anywhere on the white page to add your own ink before lifting.", mutedStyle);
        }
        else if (step == PrototypeStep.ModelLifted)
        {
            GUILayout.Label(liftElapsed > 0f ? "Your drawing is becoming spatial..." : "Transformation complete. Inspect and arrange the object.", mutedStyle);
            GUILayout.Space(4f);
            if (GUILayout.Button("Restart test  [R]", secondaryButtonStyle)) ResetPrototype();
        }
    }

    private string GetInstructionText()
    {
        switch (step)
        {
            case PrototypeStep.Welcome:
                return "Raise an L-shaped hand gesture to call your recent Notion pages into the room.";
            case PrototypeStep.DocumentPicker:
                return "Choose House Sketch from your recent pages. It will become a placeable spatial canvas.";
            case PrototypeStep.PlacingPage:
                return "Move the mouse across the detected table, then click or press Enter to anchor the page.";
            case PrototypeStep.PagePlaced:
                return "The Notion page is anchored to the detected table. Reveal its flat design sketch to continue.";
            case PrototypeStep.Drawing:
                return "Your sketch is ready. Lift it from the surface to turn the flat idea into a 3D object.";
            case PrototypeStep.ModelLifted:
                return liftElapsed > 0f
                    ? "The lifting gesture pulls the drawing upward and gives it depth."
                    : "Click and drag the house to simulate a pinch. Rotate or resize it to inspect the idea.";
            default:
                return string.Empty;
        }
    }

    private string GetControlHint()
    {
        switch (step)
        {
            case PrototypeStep.Welcome: return "L  Open page picker     •     This desktop input stands in for the proposed hand gesture";
            case PrototypeStep.DocumentPicker: return "1  Select House Sketch     •     R  Reset";
            case PrototypeStep.PlacingPage: return "Mouse  Move canvas     •     Click / Enter  Place on table     •     R  Reset";
            case PrototypeStep.PagePlaced: return "D  Reveal the 2D house sketch     •     R  Reset";
            case PrototypeStep.Drawing: return "Space  Lift into 3D     •     Click + drag  Add optional ink     •     R  Reset";
            case PrototypeStep.ModelLifted: return "Click + drag  Pinch-move     •     WASD  Nudge     •     Q / E  Rotate     •     Scroll  Resize";
            default: return string.Empty;
        }
    }
}
