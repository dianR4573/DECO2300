using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Input;

public sealed class NotionARPrototype : MonoBehaviour
{
    private enum Step { Welcome, DocumentPicker, PlacingPage, PagePlaced, Drawing, ModelLifted }

    // One row per Step: board checklist title, button label, controller hint, board instruction.
    private static readonly (string title, string button, string hint, string instruction)[] Steps =
    {
        ("Open your pages", "A", "A: Open pages", "Press A to open the page list"),
        ("Pick House Sketch", "Trigger", "Trigger: Select card", "Point at House Sketch and pull Trigger"),
        ("Anchor on table", "Trigger", "Touch table  |  Trigger: Anchor", "Touch your real table with the controller, then point at it and pull Trigger"),
        ("Reveal the sketch", "Trigger", "Trigger: Reveal sketch", "Pull Trigger to reveal the sketch"),
        ("Lift it into 3D", "Trigger", "Trigger: Lift into 3D", "Pull Trigger to lift the sketch into 3D"),
        ("Move the house", "Hold Grip", "Hold Grip: Grab & move", "Point at the house and hold Grip to move it")
    };

    private const string VirtualPlaceInstruction = "Point at the virtual table and pull Trigger to place";
    private const string HoldGripText = "Hold grip to grab";

    // Layout is in workspace space: origin on the floor under the user's head, +z where they face.
    private const float DefaultTableTopY = 0.75f;
    private const float PageLift = 0.008f;
    private const float StandingEyeHeight = 1.6f;
    private static readonly Vector3 PreviewCameraPosition = new Vector3(0f, StandingEyeHeight, -0.05f);
    // Workspace heights are laid out for a seated user whose eyes are this far above the workspace origin.
    private const float SeatedEyeHeight = 1.2f;
    // Pointer pose sits this far above the surface when the controller touches the table.
    private const float ControllerTouchOffset = 0.02f;
    private const float TableHeightSpeed = 0.4f;
    private const float LiftDuration = 1.15f;
    private const float StepRowSpacing = 0.05f;
    private const float StepListTopY = 0.02f;
    private static readonly Vector3 TableCentre = new Vector3(0f, DefaultTableTopY, 0.68f);

    // Interface palette, based on Notion's blue and neutral greys.
    private static readonly Color InkColor = new Color(0.14f, 0.51f, 0.89f);
    private static readonly Color NotionInk = new Color(0.13f, 0.13f, 0.14f);
    private static readonly Color SuccessGreen = new Color(0.18f, 0.62f, 0.42f);
    private static readonly Color MutedText = new Color(0.45f, 0.46f, 0.48f);
    private static readonly Color PanelWhite = new Color(0.98f, 0.98f, 0.97f);
    private static readonly Color PanelLine = new Color(0.88f, 0.88f, 0.86f);
    // Solid white with a dark outline so the ray reads on light tables and dark rooms in greyscale passthrough.
    private static readonly Color RayIdle = Color.white;
    private static readonly Color RayOutline = new Color(0.13f, 0.13f, 0.14f, 0.9f);
    private static readonly Color CardIdle = new Color(0.91f, 0.94f, 0.99f);
    private static readonly Color CardHover = new Color(0.68f, 0.81f, 0.98f);
    private static readonly Color CardDisabled = new Color(0.93f, 0.93f, 0.92f);
    private static readonly Color RingIdle = new Color(0.14f, 0.51f, 0.89f, 0.7f);
    private static readonly Color PassthroughClear = new Color(0f, 0f, 0f, 0f);

    // Virtual Table fallback room.
    private static readonly Color RoomSky = new Color(0.90f, 0.91f, 0.92f, 1f);
    private static readonly Color RoomWall = new Color(0.93f, 0.92f, 0.89f);
    private static readonly Color RoomFloor = new Color(0.76f, 0.73f, 0.68f);
    private static readonly Color TableTop = new Color(0.62f, 0.47f, 0.33f);
    private static readonly Color TableFrame = new Color(0.45f, 0.33f, 0.23f);

    // House model palette. No reds, so nothing reads as pink under the soft lighting.
    private static readonly Color HouseWall = new Color(0.96f, 0.94f, 0.89f);
    private static readonly Color HouseTrim = Color.white;
    private static readonly Color HouseRoof = new Color(0.27f, 0.30f, 0.35f);
    private static readonly Color HouseRoofRidge = new Color(0.20f, 0.22f, 0.26f);
    private static readonly Color HouseDoor = new Color(0.16f, 0.36f, 0.58f);
    private static readonly Color HouseGlass = new Color(0.62f, 0.80f, 0.92f);
    private static readonly Color HouseStone = new Color(0.66f, 0.65f, 0.62f);
    private static readonly Color HouseChimney = new Color(0.50f, 0.49f, 0.47f);
    private static readonly Color HouseGrass = new Color(0.52f, 0.70f, 0.44f);
    private static readonly Color HouseBrass = new Color(0.85f, 0.70f, 0.35f);

    private readonly List<GameObject> sceneRoots = new List<GameObject>();
    private readonly List<Material> createdMaterials = new List<Material>();
    private readonly Dictionary<Color, Material> litMaterials = new Dictionary<Color, Material>();
    private readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
    private readonly TextMesh[] stepRowTexts = new TextMesh[Steps.Length];

    // State.
    private Step step;
    private bool passthroughMode = true;
    private bool workspacePlaced;
    private float headTrackedTime;
    private float tableTopY = DefaultTableTopY;
    private bool lifting;
    private float liftElapsed;
    private Vector3 liftStart;
    private bool dragging;
    private bool completedFirstGrab;
    private float grabHoldTime;
    private Vector3 grabStart;
    private Vector3 dragOffset;

    // Per-frame input and pointer.
    private bool controllerTracked;
    private bool aDown, bDown, triggerDown, toggleKeyDown, gripHeld;
    private float stickY;
    private Ray ray;
    private RaycastHit hoverHit;
    private bool hasHoverHit;
    private bool pointerOnTable;
    private Vector3 tablePoint;

    private InputAction aAction, bAction, triggerAction, gripAction, stickAction, aimTrackedAction, headTrackedAction, hapticAction;

    // Scene.
    private Transform rig, workspace, headsetCamera, rightController;
    private Camera view;
    private Material lineMaterial, textMaterial, pageMaterial, pickerCardMaterial, modeToggleMaterial;
    private LineRenderer rayLine, rayOutline, reticleRing, reticleDot, reticleOutline, houseRing;
    private GameObject reticle, controllerHint, virtualRoom, tableAnchor, documentPicker, page, sketchGuide, drawingRoot, placementOutline, houseRoot, grabBadge;
    private Transform pickerCard, modeToggleButton, hintBackdrop, stepHighlight;
    private TextMesh hintText, anchorStatusText, modeToggleText, boardInstructionText, stepCounterText, pagePromptText;

    private float PageY => tableTopY + PageLift;
    // Quest 3S never reports <XRHMD>/isTracked, so also treat the headset as tracked once the pose driver
    // has moved the camera off its desk preview pose (only real tracking does that).
    private bool HeadTracked =>
        (headTrackedAction != null && headTrackedAction.IsPressed()) ||
        (headsetCamera != null && (headsetCamera.localPosition - PreviewCameraPosition).sqrMagnitude > 0.0001f);

    private void Start()
    {
        CreateInputActions();
        BuildScene();
        ResetPrototype();
    }

    private void Update()
    {
        UpdateTrackingOrigin();
        ReadInput();

        // Put the workspace in front of the user once the headset pose has been steady for a moment,
        // so the tracking origin has settled and the camera is no longer at the preview pose.
        headTrackedTime = HeadTracked ? headTrackedTime + Time.deltaTime : 0f;
        if (!workspacePlaced && headTrackedTime > 0.5f)
        {
            RecentreWorkspace();
        }

        UpdatePointer();

        if (bDown)
        {
            ResetPrototype();
            if (HeadTracked)
            {
                RecentreWorkspace();
            }

            Pulse(0.65f, 0.12f);
        }
        else if (toggleKeyDown || (triggerDown && PointingAt(modeToggleButton)))
        {
            ToggleTableMode();
            Pulse(0.35f, 0.05f);
        }
        else
        {
            HandleStep();
        }

        if (lifting)
        {
            AnimateHouseLift();
        }

        UpdateFeedback();
    }

    // Quest pauses the app when the headset comes off and refocuses it when it goes back on (or a
    // system popup closes). Recentre then, so content starts in front of whoever is wearing it rather
    // than wherever the headset was facing when it was launched from the desk.
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            workspacePlaced = false;
            headTrackedTime = 0f;
        }
    }

    private void OnDestroy()
    {
        // Edit-mode validation closes its preview scene itself; destroying again there throws.
        if (Application.isPlaying)
        {
            ClearScene();
        }

        foreach (InputAction action in new[] { aAction, bAction, triggerAction, gripAction, stickAction, aimTrackedAction, headTrackedAction, hapticAction })
        {
            action?.Dispose();
        }
    }

    // ---------------------------------------------------------------- Step flow

    private void HandleStep()
    {
        Step before = step;
        switch (step)
        {
            case Step.Welcome:
                if (aDown) OpenDocumentPicker();
                break;
            case Step.DocumentPicker:
                if (triggerDown && PointingAt(pickerCard)) BeginPagePlacement();
                break;
            case Step.PlacingPage:
                UpdatePagePlacement();
                if (triggerDown && pointerOnTable) ConfirmPagePlacement();
                break;
            case Step.PagePlaced:
                if (triggerDown) RevealSketch();
                break;
            case Step.Drawing:
                if (triggerDown) LiftSketchInto3D();
                break;
            case Step.ModelLifted:
                UpdateHouseDrag();
                break;
        }

        if (step != before)
        {
            Pulse(0.45f, 0.06f);
        }
    }

    private void SetStep(Step next)
    {
        step = next;
        SetBoardInstruction(InstructionFor(next));
    }

    private string InstructionFor(Step s)
    {
        return s == Step.PlacingPage && !passthroughMode ? VirtualPlaceInstruction : Steps[(int)s].instruction;
    }

    private void OpenDocumentPicker()
    {
        // Nothing is placed yet, so bring the workspace in front of the user: the pages always open where they look.
        if (HeadTracked)
        {
            RecentreWorkspace();
        }

        documentPicker.SetActive(true);
        SetStep(Step.DocumentPicker);
    }

    private void BeginPagePlacement()
    {
        documentPicker.SetActive(false);
        page.SetActive(true);
        sketchGuide.SetActive(false);
        drawingRoot.SetActive(false);
        placementOutline.SetActive(true);
        SetPageAlpha(0.78f);
        SetText(anchorStatusText, "TOUCH THE TABLE WITH YOUR CONTROLLER, THEN PULL TRIGGER");
        SetPagePrompt(passthroughMode ? "Point at real table + pull Trigger" : "Point at table + pull Trigger");
        SetStep(Step.PlacingPage);
        ApplyTableModeVisuals();
    }

    private void UpdatePagePlacement()
    {
        // Passthrough has no scene mesh. Touching the real table with the controller pushes the surface
        if (passthroughMode && Mathf.Abs(stickY) > 0.2f)
        {
            tableTopY = Mathf.Clamp(tableTopY + stickY * TableHeightSpeed * Time.deltaTime, 0.2f, 1.3f);
        }

        if (passthroughMode && controllerTracked)
        {
            float touchY = workspace.InverseTransformPoint(rightController.position).y - ControllerTouchOffset;
            if (touchY < tableTopY && touchY > 0.2f)
            {
                tableTopY = touchY;
            }
        }

        bool onPlane = RayOnPlane(tableTopY, out tablePoint);
        pointerOnTable = onPlane && InReach(tablePoint);
        if (!onPlane)
        {
            return;
        }

        page.transform.localPosition = new Vector3(Mathf.Clamp(tablePoint.x, -0.65f, 0.65f), PageY, Mathf.Clamp(tablePoint.z, 0.35f, 1.15f));
        tableAnchor.transform.localPosition = new Vector3(page.transform.localPosition.x, tableTopY + 0.002f, page.transform.localPosition.z);
    }

    private void ConfirmPagePlacement()
    {
        placementOutline.SetActive(false);
        SetPageAlpha(1f);
        SetText(anchorStatusText, "ANCHORED TO REAL TABLE");
        SetPagePrompt("Pull Trigger to reveal the sketch");
        SetStep(Step.PagePlaced);
        ApplyTableModeVisuals();
    }

    private void RevealSketch()
    {
        sketchGuide.SetActive(true);
        drawingRoot.SetActive(true);
        SetPagePrompt("Pull Trigger to lift into 3D");
        SetStep(Step.Drawing);
    }

    private void LiftSketchInto3D()
    {
        step = Step.ModelLifted;
        sketchGuide.SetActive(false);
        liftStart = page.transform.localPosition + new Vector3(0f, 0.005f, 0f);
        houseRoot.SetActive(true);
        houseRoot.transform.SetLocalPositionAndRotation(liftStart, Quaternion.identity);
        houseRoot.transform.localScale = Vector3.one * 0.12f;
        grabBadge.SetActive(false);
        lifting = true;
        liftElapsed = 0f;
        SetPagePrompt(HoldGripText);
        SetBoardInstruction("Lifting the sketch");
    }

    private void AnimateHouseLift()
    {
        liftElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(liftElapsed / LiftDuration);
        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        float arc = Mathf.Sin(progress * Mathf.PI);

        Transform house = houseRoot.transform;
        house.localPosition = Vector3.Lerp(liftStart, liftStart + Vector3.up * 0.22f, eased) + Vector3.up * (arc * 0.035f);
        house.localScale = Vector3.Lerp(Vector3.one * 0.12f, Vector3.one, eased);
        house.localRotation = Quaternion.Euler(0f, arc * 12f, 0f);

        if (progress > 0.42f)
        {
            drawingRoot.SetActive(false);
        }

        if (progress < 1f)
        {
            return;
        }

        // Settle onto the anchored surface, ready to grab.
        lifting = false;
        house.SetLocalPositionAndRotation(new Vector3(liftStart.x, PageY, liftStart.z), Quaternion.identity);
        grabBadge.SetActive(!completedFirstGrab);
        SetBoardInstruction(InstructionFor(Step.ModelLifted));
    }

    // The house only moves while Grip is held; releasing drops it on the surface.
    private void UpdateHouseDrag()
    {
        if (lifting)
        {
            return;
        }

        Transform house = houseRoot.transform;
        if (gripHeld && !dragging && PointerNearHouse())
        {
            dragging = true;
            grabHoldTime = 0f;
            grabStart = house.localPosition;
            dragOffset = RayOnPlane(PageY, out Vector3 point) ? house.localPosition - point : Vector3.zero;
            grabBadge.SetActive(!completedFirstGrab);
            Pulse(0.35f, 0.05f);
        }
        else if (gripHeld && dragging)
        {
            grabHoldTime += Time.deltaTime;
            if (RayOnPlane(PageY, out Vector3 point))
            {
                house.localPosition = ClampToAnchor(point + dragOffset, PageY + 0.015f);
            }
        }
        else if (!gripHeld && dragging)
        {
            dragging = false;
            house.localPosition = ClampToAnchor(house.localPosition, PageY);
            Pulse(0.2f, 0.04f);

            Vector3 moved = house.localPosition - grabStart;
            moved.y = 0f;
            // A quick tap keeps the "Hold grip to grab" prompt up; a real hold or move completes it.
            if (grabHoldTime >= 0.25f || moved.magnitude >= 0.03f)
            {
                completedFirstGrab = true;
                SetPagePrompt("House placed on table");
            }

            grabBadge.SetActive(!completedFirstGrab);
        }
    }

    private Vector3 ClampToAnchor(Vector3 local, float y)
    {
        Vector3 anchor = page.transform.localPosition;
        return new Vector3(
            Mathf.Clamp(local.x, anchor.x - 0.55f, anchor.x + 0.55f),
            y,
            Mathf.Clamp(local.z, Mathf.Max(0.25f, anchor.z - 0.40f), anchor.z + 0.45f));
    }

    public void ToggleTableMode()
    {
        passthroughMode = !passthroughMode;
        if (!passthroughMode)
        {
            // The virtual table has a fixed height, so drop any real-table adjustment.
            tableTopY = DefaultTableTopY;
            SeatOnTable();
        }

        ApplyTableModeVisuals();
        if (step == Step.PlacingPage)
        {
            SetBoardInstruction(InstructionFor(step));
        }
    }

    private void SeatOnTable()
    {
        Vector3 p = page.transform.localPosition;
        page.transform.localPosition = new Vector3(p.x, PageY, p.z);
        if (houseRoot.activeSelf && !lifting)
        {
            Vector3 h = houseRoot.transform.localPosition;
            houseRoot.transform.localPosition = new Vector3(h.x, PageY, h.z);
        }
    }

    private void ApplyTableModeVisuals()
    {
        MetaQuestPassthroughFeature.SetPassthroughActive(passthroughMode);
        view.backgroundColor = passthroughMode ? PassthroughClear : RoomSky;
        virtualRoom.SetActive(!passthroughMode);
        tableAnchor.SetActive(passthroughMode && step >= Step.PlacingPage);
        SetText(modeToggleText, passthroughMode
            ? "Mode: Real Table (Passthrough)  [Trigger: Virtual]"
            : "Mode: Virtual Table (Fallback)  [Trigger: Real]");
        FitText(modeToggleText, 0.013f, 0.46f);
    }

    private void ResetPrototype()
    {
        step = Step.Welcome;
        tableTopY = DefaultTableTopY;
        lifting = false;
        dragging = false;
        completedFirstGrab = false;

        documentPicker.SetActive(false);
        page.SetActive(false);
        page.transform.localPosition = TableCentre + Vector3.up * PageLift;
        tableAnchor.transform.localPosition = TableCentre + Vector3.up * 0.002f;
        houseRoot.SetActive(false);
        houseRoot.transform.localScale = Vector3.one;
        grabBadge.SetActive(false);

        SetPageAlpha(1f);
        SetPagePrompt("Spatial canvas ready");
        SetBoardInstruction(InstructionFor(Step.Welcome));
        ApplyTableModeVisuals();
    }

    // ---------------------------------------------------------------- Input and tracking

    private void CreateInputActions()
    {
        aAction = Enabled(new InputAction("A", InputActionType.Button, "<XRController>{RightHand}/primaryButton"));
        bAction = Enabled(new InputAction("B", InputActionType.Button, "<XRController>{RightHand}/secondaryButton"));
        triggerAction = Enabled(new InputAction("Trigger", InputActionType.Button, "<XRController>{RightHand}/triggerPressed"));
        gripAction = Enabled(new InputAction("Grip", InputActionType.Button, "<XRController>{RightHand}/gripPressed"));
        stickAction = Enabled(new InputAction("Stick", InputActionType.Value, "<XRController>{RightHand}/thumbstick", expectedControlType: "Vector2"));
        aimTrackedAction = Enabled(new InputAction("Aim tracked", InputActionType.Button, "<XRController>{RightHand}/isTracked"));
        headTrackedAction = Enabled(new InputAction("Head tracked", InputActionType.Button, "<XRHMD>/isTracked"));
        hapticAction = Enabled(new InputAction("Haptic", InputActionType.PassThrough, "<XRController>{RightHand}/haptic"));
    }

    private static InputAction Enabled(InputAction action)
    {
        action.Enable();
        return action;
    }

    private void ReadInput()
    {
        controllerTracked = aimTrackedAction.IsPressed();
        aDown = aAction.WasPressedThisFrame();
        bDown = bAction.WasPressedThisFrame();
        triggerDown = triggerAction.WasPressedThisFrame();
        gripHeld = gripAction.IsPressed();
        stickY = stickAction.ReadValue<Vector2>().y;
        toggleKeyDown = false;

        if (controllerTracked)
        {
            return;
        }

        // Editor fallback: mouse aims and clicks, keys stand in for the controller buttons.
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            aDown |= keyboard.aKey.wasPressedThisFrame;
            bDown |= keyboard.bKey.wasPressedThisFrame;
            toggleKeyDown = keyboard.vKey.wasPressedThisFrame;
            stickY += (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            triggerDown |= mouse.leftButton.wasPressedThisFrame;
            gripHeld |= mouse.rightButton.isPressed;
        }
    }

    private void Pulse(float amplitude, float seconds)
    {
        OpenXRInput.SendHapticImpulse(hapticAction, amplitude, seconds);
    }

    // Content heights assume a floor origin. Ask for one; fall back to a standing eye height.
    private void UpdateTrackingOrigin()
    {
        SubsystemManager.GetSubsystems(inputSubsystems);
        if (inputSubsystems.Count == 0)
        {
            return;
        }

        XRInputSubsystem input = inputSubsystems[0];
        if (!IsFloorBased(input.GetTrackingOriginMode()))
        {
            input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }

        rig.localPosition = IsFloorBased(input.GetTrackingOriginMode()) ? Vector3.zero : Vector3.up * StandingEyeHeight;
    }

    private static bool IsFloorBased(TrackingOriginModeFlags mode)
    {
        return mode == TrackingOriginModeFlags.Floor || mode == TrackingOriginModeFlags.Unbounded;
    }

    private void RecentreWorkspace()
    {
        Vector3 forward = Vector3.ProjectOnPlane(headsetCamera.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.ProjectOnPlane(headsetCamera.up, Vector3.up);
        }

        // Height comes from the head, not the floor: the Quest floor is wrong when no boundary is set
        // up for the room, which put the whole workspace above the user.
        Vector3 head = headsetCamera.position;
        workspace.SetPositionAndRotation(new Vector3(head.x, head.y - SeatedEyeHeight, head.z), Quaternion.LookRotation(forward.normalized, Vector3.up));
        workspacePlaced = true;
        Debug.Log("[NotionAR] Recentred. Head " + head + ", workspace " + workspace.position + ", rig " + rig.localPosition);
    }

    // ---------------------------------------------------------------- Pointer

    private void UpdatePointer()
    {
        ray = PointerRay();
        hasHoverHit = Physics.Raycast(ray, out hoverHit, 6f);
        pointerOnTable = RayOnPlane(tableTopY, out tablePoint) && InReach(tablePoint);
    }

    private Ray PointerRay()
    {
        if (controllerTracked)
        {
            return new Ray(rightController.position, rightController.forward);
        }

        Mouse mouse = Mouse.current;
        return mouse != null ? view.ScreenPointToRay(mouse.position.ReadValue()) : new Ray(headsetCamera.position, headsetCamera.forward);
    }

    private bool PointingAt(Transform target)
    {
        return hasHoverHit && target != null && hoverHit.transform.IsChildOf(target);
    }

    // Where the pointer meets a horizontal plane, in workspace space.
    private bool RayOnPlane(float height, out Vector3 local)
    {
        // Height is in workspace space; the workspace sits at head height minus SeatedEyeHeight, not at y = 0.
        Plane plane = new Plane(Vector3.up, workspace.TransformPoint(new Vector3(0f, height, 0f)));
        if (plane.Raycast(ray, out float distance) && distance < 6f)
        {
            local = workspace.InverseTransformPoint(ray.GetPoint(distance));
            return true;
        }

        local = default;
        return false;
    }

    private static bool InReach(Vector3 local)
    {
        return Mathf.Abs(local.x) <= 0.75f && local.z >= 0.25f && local.z <= 1.3f;
    }

    private bool PointerNearHouse()
    {
        if (PointingAt(houseRoot.transform))
        {
            return true;
        }

        if (!RayOnPlane(PageY, out Vector3 point))
        {
            return false;
        }

        Vector3 flat = point - houseRoot.transform.localPosition;
        flat.y = 0f;
        return flat.magnitude < 0.25f;
    }

    // True when pulling the trigger (or holding grip) right now would do something.
    private bool PointerIsActionable()
    {
        if (PointingAt(modeToggleButton))
        {
            return true;
        }

        switch (step)
        {
            case Step.DocumentPicker: return PointingAt(pickerCard);
            case Step.PlacingPage: return pointerOnTable;
            case Step.PagePlaced:
            case Step.Drawing: return true;
            case Step.ModelLifted: return !lifting && (dragging || PointerNearHouse());
            default: return false;
        }
    }

    private void UpdateFeedback()
    {
        bool actionable = PointerIsActionable();
        bool onToggle = PointingAt(modeToggleButton);
        Color feedback = actionable ? InkColor : RayIdle;

        // Hover highlights.
        bool hoverCard = step == Step.DocumentPicker && PointingAt(pickerCard);
        SetMaterialColor(pickerCardMaterial, hoverCard ? CardHover : CardIdle);
        pickerCard.localScale = new Vector3(hoverCard ? 0.70f : 0.68f, hoverCard ? 0.10f : 0.095f, 0.015f);
        SetMaterialColor(modeToggleMaterial, onToggle ? CardHover : CardIdle);
        SetLineColor(houseRing, actionable && step == Step.ModelLifted && !onToggle ? SuccessGreen : RingIdle);

        // Reticle on whatever the pointer hits, or on the table surface.
        bool showReticle = hasHoverHit || pointerOnTable;
        reticle.SetActive(showReticle);
        if (showReticle)
        {
            Vector3 point = hasHoverHit ? hoverHit.point : workspace.TransformPoint(tablePoint);
            Vector3 normal = hasHoverHit ? hoverHit.normal : Vector3.up;
            float distance = Vector3.Distance(ray.origin, point);
            Vector3 up = Mathf.Abs(normal.y) > 0.9f ? workspace.forward : Vector3.up;
            reticle.transform.SetPositionAndRotation(point + normal * 0.003f, Quaternion.LookRotation(-normal, up));
            reticle.transform.localScale = Vector3.one * Mathf.Clamp(distance, 0.5f, 2.5f) * (actionable ? 1.35f : 1f);
            SetLineColor(reticleRing, feedback);
            SetLineColor(reticleDot, feedback);
        }

        // Controller ray.
        rayLine.enabled = controllerTracked;
        rayOutline.enabled = controllerTracked;
        if (controllerTracked)
        {
            float length = showReticle ? Mathf.Max(0.05f, Vector3.Distance(ray.origin, reticle.transform.position)) : 2.5f;
            rayLine.SetPosition(1, Vector3.forward * length);
            rayOutline.SetPosition(1, Vector3.forward * length);
            SetLineColor(rayLine, feedback);
        }

        UpdateControllerHint();
        Billboard(grabBadge.transform, true);
    }

    // First-run overlay near the controller with the button for the current step.
    private void UpdateControllerHint()
    {
        bool show = controllerTracked && !lifting && (step != Step.ModelLifted || !completedFirstGrab);
        controllerHint.SetActive(show);
        if (!show)
        {
            return;
        }

        string hint = step == Step.PlacingPage && !passthroughMode ? "Trigger: Place on table" : Steps[(int)step].hint;
        if (hintText.text != hint)
        {
            SetText(hintText, hint);
            FitText(hintText, 0.011f, 0f);
            hintBackdrop.localScale = new Vector3(hintText.GetComponent<MeshRenderer>().localBounds.size.x + 0.024f, 0.028f, 0.002f);
        }

        controllerHint.transform.position = rightController.position + Vector3.up * 0.07f;
        Billboard(controllerHint.transform, false);
    }

    private void Billboard(Transform target, bool upright)
    {
        if (!target.gameObject.activeInHierarchy)
        {
            return;
        }

        Vector3 away = target.position - headsetCamera.position;
        if (upright)
        {
            away.y = 0f;
        }

        if (away.sqrMagnitude > 0.0001f)
        {
            target.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }

    // ---------------------------------------------------------------- Scene building

    private void BuildScene()
    {
        ClearScene();
        lineMaterial = NewMaterial("DECO2300/VertexColor");
        textMaterial = NewMaterial("DECO2300/TextUnlit");

        CreateRig();
        // Inside the rig, so a tracking-origin change that moves the camera moves the content with it.
        // As a separate root, a late Floor/Device switch left everything 1.6 m away from the user.
        workspace = new GameObject("Workspace").transform;
        workspace.SetParent(rig, false);
        BuildPointer();
        BuildEnvironment();
        BuildDocumentPicker();
        BuildPage();
        BuildHouseModel();
    }

    private void CreateRig()
    {
        foreach (Camera stray in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            stray.tag = "Untagged";
            stray.enabled = false;
            AudioListener listener = stray.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        rig = NewRoot("XR Origin").transform;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(rig, false);
        // Desk preview pose; the pose driver takes over as soon as a headset is tracked.
        cameraObject.transform.SetLocalPositionAndRotation(PreviewCameraPosition, Quaternion.Euler(42f, 0f, 0f));
        view = cameraObject.AddComponent<Camera>();
        view.clearFlags = CameraClearFlags.SolidColor;
        view.allowHDR = false;
        view.nearClipPlane = 0.05f;
        view.farClipPlane = 40f;
        view.fieldOfView = 60f;
        cameraObject.AddComponent<AudioListener>();
        AddPoseDriver(cameraObject, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", "<XRHMD>/trackingState");
        headsetCamera = cameraObject.transform;

        GameObject controller = new GameObject("Right Controller");
        controller.transform.SetParent(rig, false);
        AddPoseDriver(controller, "<XRController>{RightHand}/pointerPosition", "<XRController>{RightHand}/pointerRotation", "<XRController>{RightHand}/trackingState");
        rightController = controller.transform;
    }

    private static void AddPoseDriver(GameObject target, string position, string rotation, string trackingState)
    {
        TrackedPoseDriver driver = target.AddComponent<TrackedPoseDriver>();
        driver.positionInput = new InputActionProperty(new InputAction(binding: position, expectedControlType: "Vector3"));
        driver.rotationInput = new InputActionProperty(new InputAction(binding: rotation, expectedControlType: "Quaternion"));
        driver.trackingStateInput = new InputActionProperty(new InputAction(binding: trackingState, expectedControlType: "Integer"));
    }

    private void BuildPointer()
    {
        // Outlines are wider dark copies drawn first (lower sortingOrder), so the coloured core sits on top.
        rayOutline = CreateLine("Controller Ray Outline", rightController, new[] { Vector3.zero, Vector3.forward * 2f }, 0.014f, RayOutline, false);
        rayOutline.endWidth = 0.008f;
        rayLine = CreateLine("Controller Ray Pointer", rightController, new[] { Vector3.zero, Vector3.forward * 2f }, 0.008f, RayIdle, false);
        rayLine.endWidth = 0.004f;
        rayLine.sortingOrder = 1;

        reticle = NewRoot("Pointer reticle");
        reticleOutline = CreateLine("Reticle outline", reticle.transform, Circle(0.014f), 0.009f, RayOutline, true);
        reticleRing = CreateLine("Reticle ring", reticle.transform, Circle(0.014f), 0.005f, Color.white, true);
        reticleDot = CreateLine("Reticle dot", reticle.transform, Circle(0.0025f), 0.007f, Color.white, true);
        reticleOutline.alignment = LineAlignment.TransformZ;
        reticleRing.alignment = LineAlignment.TransformZ;
        reticleDot.alignment = LineAlignment.TransformZ;
        reticleRing.sortingOrder = 1;
        reticleDot.sortingOrder = 1;

        controllerHint = NewRoot("Controller hint");
        hintBackdrop = CreatePrimitive(PrimitiveType.Cube, "Hint backdrop", controllerHint.transform, new Vector3(0f, 0f, 0.002f), new Vector3(0.14f, 0.028f, 0.002f), Lit(NotionInk), false).transform;
        hintText = CreateText("controller-hint", Steps[0].hint, controllerHint.transform, Vector3.zero, 0.011f, TextAnchor.MiddleCenter, Color.white, false);
    }

    private void BuildEnvironment()
    {
        Transform environment = new GameObject("Mixed-reality room").transform;
        environment.SetParent(workspace, false);

        // Virtual Table fallback: hidden while passthrough shows the real room.
        virtualRoom = new GameObject("Virtual room fallback group");
        virtualRoom.transform.SetParent(environment, false);
        Transform room = virtualRoom.transform;
        Cube("Tabletop / detected surface", room, new Vector3(0f, 0.73f, 0.75f), new Vector3(1.40f, 0.04f, 0.80f), TableTop, true);
        Cube("Table front apron", room, new Vector3(0f, 0.69f, 0.36f), new Vector3(1.36f, 0.04f, 0.03f), TableFrame);
        foreach (Vector2 leg in new[] { new Vector2(-0.64f, 0.40f), new Vector2(0.64f, 0.40f), new Vector2(-0.64f, 1.10f), new Vector2(0.64f, 1.10f) })
        {
            Cube("Table leg", room, new Vector3(leg.x, 0.355f, leg.y), new Vector3(0.06f, 0.71f, 0.06f), TableFrame);
        }

        Cube("Floor", room, new Vector3(0f, -0.02f, 1.2f), new Vector3(8f, 0.04f, 8f), RoomFloor, true);
        Cube("Back wall", room, new Vector3(0f, 1.75f, 2.4f), new Vector3(8f, 3.5f, 0.08f), RoomWall, true);

        // Real table anchor: follows the page while placing, then marks the anchored surface.
        tableAnchor = new GameObject("Real table surface anchor");
        tableAnchor.transform.SetParent(environment, false);
        CreateLine("Real table anchor boundary", tableAnchor.transform, Rectangle(0.48f, 0.30f, 0.002f), 0.004f, SuccessGreen, true);
        anchorStatusText = CreateText("real-table-status", "", tableAnchor.transform, new Vector3(0f, 0.003f, -0.26f), 0.012f, TextAnchor.MiddleCenter, SuccessGreen, true);

        BuildInstructionBoard(environment);
    }

    // Instructions sit on a panel to the left, angled toward the user, so the table stays clear.
    private void BuildInstructionBoard(Transform parent)
    {
        Transform board = new GameObject("Room instruction board").transform;
        board.SetParent(parent, false);
        board.SetLocalPositionAndRotation(new Vector3(-1.0f, 1.18f, 0.92f), Quaternion.Euler(0f, -45f, 0f));
        // Scaled as a whole so every label grows together and stays readable at arm's length.
        board.localScale = Vector3.one * 1.4f;

        const float left = -0.24f;
        Cube("Board backdrop", board, Vector3.zero, new Vector3(0.56f, 0.82f, 0.02f), PanelWhite, true);
        Cube("Board accent", board, new Vector3(0f, 0.395f, -0.011f), new Vector3(0.56f, 0.03f, 0.004f), InkColor);
        CreateText("board-title", "NOTION AR", board, new Vector3(left, 0.35f, -0.02f), 0.022f, TextAnchor.UpperLeft, NotionInk, false);
        stepCounterText = CreateText("board-step-counter", "STEP 1 OF 6", board, new Vector3(-left, 0.35f, -0.02f), 0.016f, TextAnchor.UpperRight, MutedText, false);
        boardInstructionText = CreateText("board-instruction", "", board, new Vector3(left, 0.28f, -0.02f), 0.03f, TextAnchor.UpperLeft, InkColor, false);
        boardInstructionText.fontStyle = FontStyle.Bold;
        Cube("Board divider", board, new Vector3(0f, 0.06f, -0.011f), new Vector3(0.48f, 0.003f, 0.002f), PanelLine);

        stepHighlight = Cube("Current step highlight", board, new Vector3(0f, StepListTopY, -0.012f), new Vector3(0.50f, 0.042f, 0.002f), CardIdle).transform;
        for (int i = 0; i < Steps.Length; i++)
        {
            float y = StepListTopY - i * StepRowSpacing;
            stepRowTexts[i] = CreateText("board-step-" + (i + 1), (i + 1) + "   " + Steps[i].title, board, new Vector3(left, y, -0.02f), 0.02f, TextAnchor.MiddleLeft, MutedText, false);
            CreateText("board-step-button-" + (i + 1), Steps[i].button, board, new Vector3(-left, y, -0.02f), 0.018f, TextAnchor.MiddleRight, MutedText, false);
        }

        CreateText("board-restart-hint", "B  reset & recentre in front of you", board, new Vector3(left, -0.29f, -0.02f), 0.015f, TextAnchor.MiddleLeft, MutedText, false);

        modeToggleMaterial = NewLit(CardIdle);
        modeToggleButton = CreatePrimitive(PrimitiveType.Cube, "Mode toggle button", board, new Vector3(0f, -0.355f, -0.015f), new Vector3(0.50f, 0.045f, 0.012f), modeToggleMaterial, true).transform;
        modeToggleText = CreateText("mode-toggle-label", "", board, new Vector3(0f, -0.355f, -0.025f), 0.013f, TextAnchor.MiddleCenter, NotionInk, false);
    }

    private void SetBoardInstruction(string instruction)
    {
        SetWrappedText(boardInstructionText, instruction, 0.03f, 0.46f, 24);

        int current = (int)step;
        SetText(stepCounterText, "STEP " + (current + 1) + " OF " + Steps.Length);
        for (int i = 0; i < stepRowTexts.Length; i++)
        {
            stepRowTexts[i].color = Display(i < current ? SuccessGreen : i == current ? NotionInk : MutedText);
            stepRowTexts[i].fontStyle = i == current ? FontStyle.Bold : FontStyle.Normal;
            RefreshText(stepRowTexts[i]);
        }

        Vector3 p = stepHighlight.localPosition;
        stepHighlight.localPosition = new Vector3(p.x, StepListTopY - current * StepRowSpacing, p.z);
    }

    private void BuildDocumentPicker()
    {
        documentPicker = new GameObject("Floating Notion page picker");
        documentPicker.transform.SetParent(workspace, false);
        documentPicker.transform.localPosition = new Vector3(0f, 1.30f, 0.78f);
        Transform picker = documentPicker.transform;

        Cube("Picker backdrop", picker, new Vector3(0f, -0.03f, 0f), new Vector3(0.76f, 0.52f, 0.02f), PanelWhite);
        CreateText("picker-eyebrow", "NOTION  /  RECENT", picker, new Vector3(0f, 0.18f, -0.02f), 0.02f, TextAnchor.MiddleCenter, MutedText, false);
        CreateText("picker-title", "Choose a canvas", picker, new Vector3(0f, 0.12f, -0.02f), 0.036f, TextAnchor.MiddleCenter, NotionInk, false);

        (string title, string detail)[] cards = { ("House Sketch", "Edited today"), ("Studio Notes", "Text page"), ("Material Study", "Gallery") };
        for (int i = 0; i < cards.Length; i++)
        {
            float y = 0.02f - i * 0.11f;
            // Only House Sketch is selectable: it gets its own material (for hover) and a collider.
            bool selectable = i == 0;
            Material material = selectable ? NewLit(CardIdle) : Lit(CardDisabled);
            GameObject card = CreatePrimitive(PrimitiveType.Cube, "Page option " + (i + 1) + " - " + cards[i].title, picker, new Vector3(0f, y, -0.02f), new Vector3(0.68f, 0.095f, 0.015f), material, selectable);
            if (selectable)
            {
                pickerCard = card.transform;
                pickerCardMaterial = material;
            }
            CreateText("option-title-" + (i + 1), (i + 1) + "   " + cards[i].title, picker, new Vector3(-0.31f, y + 0.012f, -0.035f), 0.032f, TextAnchor.MiddleLeft, NotionInk, false);
            CreateText("option-detail-" + (i + 1), cards[i].detail, picker, new Vector3(0.31f, y + 0.012f, -0.035f), 0.022f, TextAnchor.MiddleRight, MutedText, false);
        }
    }

    private void BuildPage()
    {
        page = new GameObject("Notion page canvas");
        page.transform.SetParent(workspace, false);
        Transform canvas = page.transform;

        pageMaterial = NewLit(PanelWhite);
        pageMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        pageMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        pageMaterial.SetFloat("_ZWrite", 0f);
        pageMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        CreatePrimitive(PrimitiveType.Cube, "Page surface", canvas, Vector3.zero, new Vector3(0.55f, 0.008f, 0.38f), pageMaterial, false);

        CreateText("page-kicker", "NOTION  /  DESIGN STUDY", canvas, new Vector3(-0.24f, 0.01f, 0.14f), 0.012f, TextAnchor.UpperLeft, MutedText, true);
        CreateText("page-title", "House Sketch", canvas, new Vector3(-0.24f, 0.01f, 0.09f), 0.022f, TextAnchor.UpperLeft, NotionInk, true);
        pagePromptText = CreateText("page-prompt", "", canvas, new Vector3(-0.24f, 0.01f, 0.04f), 0.014f, TextAnchor.UpperLeft, MutedText, true);

        Vector3[] houseOutline =
        {
            new Vector3(-0.11f, 0.012f, -0.13f), new Vector3(0.11f, 0.012f, -0.13f), new Vector3(0.11f, 0.012f, -0.01f),
            new Vector3(0f, 0.012f, 0.09f), new Vector3(-0.11f, 0.012f, -0.01f), new Vector3(-0.11f, 0.012f, -0.13f)
        };

        sketchGuide = new GameObject("Faint 2D house guide");
        sketchGuide.transform.SetParent(canvas, false);
        CreateLine("guide-house-outline", sketchGuide.transform, houseOutline, 0.004f, new Color(0.68f, 0.70f, 0.72f, 0.72f), false);

        // The pre-made sketch the reveal shows: house outline plus a door.
        drawingRoot = new GameObject("User drawing strokes");
        drawingRoot.transform.SetParent(canvas, false);
        CreateLine("Sketch stroke 1", drawingRoot.transform, houseOutline, 0.006f, InkColor, false);
        CreateLine("Sketch stroke 2", drawingRoot.transform, new[]
        {
            new Vector3(-0.025f, 0.012f, -0.13f), new Vector3(-0.025f, 0.012f, -0.065f),
            new Vector3(0.025f, 0.012f, -0.065f), new Vector3(0.025f, 0.012f, -0.13f)
        }, 0.006f, InkColor, false);

        placementOutline = CreateLine("Placement boundary", canvas, Rectangle(0.29f, 0.20f, 0.012f), 0.006f, SuccessGreen, true).gameObject;
    }

    // A small architectural model: grass base, stone plinth, warm white walls, slate roof,
    // framed windows, a blue front door and a stone chimney. Front faces the user (-z).
    private void BuildHouseModel()
    {
        houseRoot = new GameObject("3D object generated from the sketch");
        houseRoot.transform.SetParent(workspace, false);
        Transform house = houseRoot.transform;

        Cube("Grass base", house, new Vector3(0f, 0.004f, -0.01f), new Vector3(0.30f, 0.008f, 0.27f), HouseGrass, true);
        Cube("Stone plinth", house, new Vector3(0f, 0.014f, 0f), new Vector3(0.236f, 0.012f, 0.196f), HouseStone, true);
        Cube("Front path", house, new Vector3(0f, 0.009f, -0.12f), new Vector3(0.05f, 0.003f, 0.06f), HouseStone);

        const float wallBottom = 0.02f;
        const float wallHeight = 0.14f;
        const float halfWidth = 0.11f;
        const float halfDepth = 0.09f;
        const float front = -halfDepth - 0.002f;
        const float wallMid = wallBottom + wallHeight * 0.5f;

        Cube("House body", house, new Vector3(0f, wallMid, 0f), new Vector3(halfWidth * 2f, wallHeight, halfDepth * 2f), HouseWall, true);
        Cube("Corner trim left", house, new Vector3(-halfWidth, wallMid, front), new Vector3(0.008f, wallHeight, 0.006f), HouseTrim);
        Cube("Corner trim right", house, new Vector3(halfWidth, wallMid, front), new Vector3(0.008f, wallHeight, 0.006f), HouseTrim);
        CreateGableRoof(house, wallBottom + wallHeight, halfWidth, halfDepth);

        Cube("Door frame", house, new Vector3(0f, wallBottom + 0.05f, front), new Vector3(0.056f, 0.10f, 0.004f), HouseTrim);
        Cube("Front door", house, new Vector3(0f, wallBottom + 0.047f, front - 0.002f), new Vector3(0.044f, 0.094f, 0.004f), HouseDoor, true);
        CreatePrimitive(PrimitiveType.Sphere, "Door knob", house, new Vector3(0.013f, wallBottom + 0.045f, front - 0.005f), Vector3.one * 0.006f, Lit(HouseBrass), false);
        Cube("Door step", house, new Vector3(0f, wallBottom + 0.003f, front - 0.012f), new Vector3(0.07f, 0.006f, 0.022f), HouseStone);

        CreateWindow("Left window", house, new Vector3(-0.065f, wallBottom + 0.085f, front), 0f);
        CreateWindow("Right window", house, new Vector3(0.065f, wallBottom + 0.085f, front), 0f);
        CreateWindow("Side window left", house, new Vector3(-halfWidth - 0.002f, wallBottom + 0.075f, 0f), 90f);
        CreateWindow("Side window right", house, new Vector3(halfWidth + 0.002f, wallBottom + 0.075f, 0f), -90f);

        Cube("Chimney", house, new Vector3(0.062f, 0.23f, 0.035f), new Vector3(0.03f, 0.075f, 0.034f), HouseChimney, true);
        Cube("Chimney cap", house, new Vector3(0.062f, 0.27f, 0.035f), new Vector3(0.038f, 0.008f, 0.042f), HouseStone);

        houseRing = CreateLine("Selection ring", house, Ellipse(0.20f, 0.18f, 0.004f, -0.01f, 48), 0.004f, RingIdle, true);

        grabBadge = new GameObject("Grab prompt badge");
        grabBadge.transform.SetParent(house, false);
        grabBadge.transform.localPosition = new Vector3(0f, 0.34f, 0f);
        Cube("Grab prompt backdrop", grabBadge.transform, new Vector3(0f, 0f, 0.002f), new Vector3(0.24f, 0.036f, 0.003f), NotionInk);
        CreateText("grab-prompt", HoldGripText, grabBadge.transform, Vector3.zero, 0.015f, TextAnchor.MiddleCenter, Color.white, false);
    }

    // Window seen from its local -z side: white frame, pale blue glass, cross mullions.
    private void CreateWindow(string objectName, Transform parent, Vector3 localPosition, float yaw)
    {
        Transform window = new GameObject(objectName).transform;
        window.SetParent(parent, false);
        window.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(0f, yaw, 0f));
        Cube("Window frame", window, Vector3.zero, new Vector3(0.046f, 0.046f, 0.004f), HouseTrim);
        Cube("Window glass", window, new Vector3(0f, 0f, -0.002f), new Vector3(0.036f, 0.036f, 0.002f), HouseGlass);
        Cube("Mullion vertical", window, new Vector3(0f, 0f, -0.0035f), new Vector3(0.003f, 0.036f, 0.002f), HouseTrim);
        Cube("Mullion horizontal", window, new Vector3(0f, 0f, -0.0035f), new Vector3(0.036f, 0.003f, 0.002f), HouseTrim);
        Cube("Window sill", window, new Vector3(0f, -0.026f, -0.004f), new Vector3(0.054f, 0.005f, 0.01f), HouseTrim);
    }

    // Two slate roof slabs with an overhang, over wall-coloured gable triangles.
    private void CreateGableRoof(Transform parent, float eaveY, float wallHalfWidth, float wallHalfDepth)
    {
        Transform roof = new GameObject("Gable roof").transform;
        roof.SetParent(parent, false);
        roof.localPosition = new Vector3(0f, eaveY, 0f);

        const float pitchDegrees = 36f;
        const float overhang = 0.022f;
        float pitch = pitchDegrees * Mathf.Deg2Rad;
        float ridgeY = Mathf.Tan(pitch) * wallHalfWidth;

        Mesh mesh = new Mesh { name = "Generated gable ends" };
        mesh.vertices = new[]
        {
            new Vector3(-wallHalfWidth, 0f, -wallHalfDepth), new Vector3(wallHalfWidth, 0f, -wallHalfDepth), new Vector3(0f, ridgeY, -wallHalfDepth),
            new Vector3(-wallHalfWidth, 0f, wallHalfDepth), new Vector3(wallHalfWidth, 0f, wallHalfDepth), new Vector3(0f, ridgeY, wallHalfDepth)
        };
        mesh.triangles = new[] { 0, 2, 1, 3, 4, 5 };
        mesh.RecalculateNormals();
        GameObject gables = new GameObject("Gable ends");
        gables.transform.SetParent(roof, false);
        gables.AddComponent<MeshFilter>().sharedMesh = mesh;
        gables.AddComponent<MeshRenderer>().sharedMaterial = Lit(HouseWall);

        float halfSpan = wallHalfWidth + overhang;
        Vector3 slabScale = new Vector3(halfSpan / Mathf.Cos(pitch) + 0.006f, 0.012f, (wallHalfDepth + overhang) * 2f);
        for (int side = -1; side <= 1; side += 2)
        {
            // Each slab runs from the ridge down past the wall by the overhang.
            Vector3 centre = new Vector3(side * halfSpan * 0.5f, ridgeY - Mathf.Tan(pitch) * halfSpan * 0.5f + 0.006f, 0f);
            GameObject slab = Cube(side < 0 ? "Roof slab left" : "Roof slab right", roof, centre, slabScale, HouseRoof, true);
            slab.transform.localRotation = Quaternion.Euler(0f, 0f, -side * pitchDegrees);
        }

        Cube("Roof ridge", roof, new Vector3(0f, ridgeY + 0.012f, 0f), new Vector3(0.012f, 0.008f, slabScale.z), HouseRoofRidge);
    }

    // ---------------------------------------------------------------- Primitives

    private GameObject NewRoot(string objectName)
    {
        GameObject root = new GameObject(objectName);
        sceneRoots.Add(root);
        return root;
    }

    private GameObject Cube(string objectName, Transform parent, Vector3 localPosition, Vector3 scale, Color color, bool solid = false)
    {
        return CreatePrimitive(PrimitiveType.Cube, objectName, parent, localPosition, scale, Lit(color), solid);
    }

    // Solid primitives keep their collider so the pointer can hit them.
    private static GameObject CreatePrimitive(PrimitiveType type, string objectName, Transform parent, Vector3 localPosition, Vector3 scale, Material material, bool solid)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = objectName;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localScale = scale;
        primitive.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid)
        {
            DestroyRuntimeObject(primitive.GetComponent<Collider>());
        }

        return primitive;
    }

    private LineRenderer CreateLine(string objectName, Transform parent, Vector3[] localPoints, float width, Color color, bool loop)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = loop;
        line.positionCount = localPoints.Length;
        line.SetPositions(localPoints);
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 3;
        line.numCornerVertices = 3;
        line.sharedMaterial = lineMaterial;
        SetLineColor(line, color);
        return line;
    }

    // Flat ellipse on the XZ plane at height y.
    private static Vector3[] Ellipse(float radiusX, float radiusZ, float y, float offsetZ, int count)
    {
        Vector3[] points = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            points[i] = new Vector3(Mathf.Cos(angle) * radiusX, y, Mathf.Sin(angle) * radiusZ + offsetZ);
        }

        return points;
    }

    // Circle on the XY plane, facing along z (used by the reticle).
    private static Vector3[] Circle(float radius)
    {
        Vector3[] points = Ellipse(radius, radius, 0f, 0f, 24);
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Vector3(points[i].x, points[i].z, 0f);
        }

        return points;
    }

    private static Vector3[] Rectangle(float halfX, float halfZ, float y)
    {
        return new[] { new Vector3(-halfX, y, -halfZ), new Vector3(halfX, y, -halfZ), new Vector3(halfX, y, halfZ), new Vector3(-halfX, y, halfZ) };
    }

    // ---------------------------------------------------------------- Text

    private TextMesh CreateText(string objectName, string text, Transform parent, Vector3 localPosition, float heightMetres, TextAnchor anchor, Color color, bool layFlat)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.transform.SetLocalPositionAndRotation(localPosition, layFlat ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity);
        TextMesh mesh = textObject.AddComponent<TextMesh>();
        mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mesh.fontSize = 96;
        mesh.anchor = anchor;
        mesh.color = Display(color);
        mesh.text = text;
        FitText(mesh, heightMetres, 0f);
        RefreshText(mesh);
        return mesh;
    }

    private void SetText(TextMesh mesh, string text)
    {
        mesh.text = text;
        RefreshText(mesh);
    }

    private void SetPagePrompt(string text)
    {
        SetText(pagePromptText, text);
    }

    // characterSize is scaled from the mesh bounds so the requested height is in metres.
    // A max width above zero shrinks long text so it fits.
    private static void FitText(TextMesh mesh, float heightMetres, float maxWidth)
    {
        mesh.font.RequestCharactersInTexture(mesh.text, mesh.fontSize, mesh.fontStyle);
        mesh.characterSize = 1f;
        Vector3 size = mesh.GetComponent<MeshRenderer>().localBounds.size;
        float scale = size.y > 0.001f ? heightMetres / size.y : heightMetres;
        if (maxWidth > 0f && size.x * scale > maxWidth)
        {
            scale *= maxWidth / (size.x * scale);
        }

        mesh.characterSize = scale;
    }

    // TextMesh has no word wrap. Break by words, size by one line's height, then shrink if too wide.
    private void SetWrappedText(TextMesh mesh, string text, float lineHeight, float maxWidth, int charsPerLine)
    {
        mesh.text = "Ag";
        FitText(mesh, lineHeight, 0f);

        System.Text.StringBuilder wrapped = new System.Text.StringBuilder();
        int lineLength = 0;
        foreach (string word in text.Split(' '))
        {
            if (lineLength > 0)
            {
                bool newLine = lineLength + 1 + word.Length > charsPerLine;
                wrapped.Append(newLine ? '\n' : ' ');
                lineLength = newLine ? 0 : lineLength + 1;
            }

            wrapped.Append(word);
            lineLength += word.Length;
        }

        SetText(mesh, wrapped.ToString());
        float width = mesh.GetComponent<MeshRenderer>().localBounds.size.x;
        if (width > maxWidth)
        {
            mesh.characterSize *= maxWidth / width;
        }
    }

    private void RefreshText(TextMesh mesh)
    {
        mesh.font.RequestCharactersInTexture(mesh.text, mesh.fontSize, mesh.fontStyle);
        textMaterial.mainTexture = mesh.font.material.mainTexture;
        mesh.GetComponent<MeshRenderer>().sharedMaterial = textMaterial;
    }

    // ---------------------------------------------------------------- Materials and colour

    // The DECO2300 shaders are in Always Included Shaders; URP Lit is stripped from Quest builds.
    private Material NewMaterial(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError("[NotionAR] Missing shader " + shaderName + ". Add it to Always Included Shaders.");
            shader = Shader.Find("Hidden/InternalErrorShader");
        }

        Material material = new Material(shader);
        createdMaterials.Add(material);
        return material;
    }

    private Material NewLit(Color color)
    {
        Material material = NewMaterial("DECO2300/SimpleLit");
        material.SetColor("_BaseColor", color);
        return material;
    }

    // Shared material per colour, for everything that never changes colour.
    private Material Lit(Color color)
    {
        if (!litMaterials.TryGetValue(color, out Material material))
        {
            material = NewLit(color);
            litMaterials[color] = material;
        }

        return material;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        material.SetColor("_BaseColor", color);
    }

    private void SetPageAlpha(float alpha)
    {
        Color color = PanelWhite;
        color.a = alpha;
        pageMaterial.SetColor("_BaseColor", color);
    }

    private static void SetLineColor(LineRenderer line, Color color)
    {
        line.startColor = Display(color);
        line.endColor = Display(color);
    }

    // TextMesh and LineRenderer colours are vertex colours, which Unity does not convert in Linear
    // colour space. Without this, text and lines come out much paler than the colour picked.
    private static Color Display(Color color)
    {
        return QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
    }

    // ---------------------------------------------------------------- Cleanup

    private void ClearScene()
    {
        foreach (GameObject root in sceneRoots)
        {
            DestroyRuntimeObject(root);
        }

        foreach (Material material in createdMaterials)
        {
            DestroyRuntimeObject(material);
        }

        sceneRoots.Clear();
        createdMaterials.Clear();
        litMaterials.Clear();
    }

    private static void DestroyRuntimeObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
