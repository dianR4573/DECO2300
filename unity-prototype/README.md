# Notion AR — Unity Interactive Prototype 1

This Unity project contains the Phase 1 horizontal / experiential prototype for **DECO2300: Notion AR — Turning 2D Notes into 3D Spatial Ideas**.

The prototype simulates a future Meta Quest / mixed-reality Notion interaction in a desktop Unity scene. It is intentionally not a full Notion clone and does not implement real AR passthrough, hand tracking, or automatic 2D-to-3D model generation.

## Prototype flow

1. Press `L` to simulate the L-shaped hand gesture and open a floating Notion-style document picker.
2. Press `1` to select the **House Sketch** page and place it on the table.
3. Press `D` to reveal the 2D sketch on the page.
4. Press `Space` to simulate lifting the sketch into a 3D object.
5. Use `WASD` or arrow keys to move the 3D object.
6. Press `R` to reset the prototype.

## How to open

1. Open Unity Hub.
2. Add/open the folder: `unity-prototype/`.
3. Open `Assets/Scenes/MainScene_NotionAR.unity`.
4. Press Play.

If Unity asks to regenerate project files or upgrade the project version, allow it.

## If the scene appears empty

Open the Unity menu:

`Tools > DECO2300 > Rebuild Notion AR Main Scene`

This editor utility creates the camera, light, and prototype controller, then saves the main scene.

## Assessment focus

This is a **horizontal / experiential prototype**. It gives users an overall impression of the concept and supports testing of the key design assumptions:

- Is the table-based Notion workspace understandable?
- Does the 2D-to-3D transformation feel useful?
- Is the simulated lifting interaction clear enough?
- Can users understand moving the 3D object in space?

## Files

- `Assets/Scripts/NotionARPrototype.cs` — runtime prototype logic and generated scene objects.
- `Assets/Editor/NotionARSceneBuilder.cs` — editor utility for rebuilding the scene.
- `Assets/Scenes/MainScene_NotionAR.unity` — main scene.
