# Notion AR — Unity Interactive Prototype 2b (Meta Quest Mixed Reality Build)

This Unity project contains **Interactive Prototype 2b** for **DECO2300: Notion AR — Turning 2D Notes into 3D Spatial Ideas**.

It upgrades the IP2a Meta Quest build with **mixed reality passthrough**, **real table anchoring**, a **Virtual Table fallback toggle**, a **one-job-per-button control scheme**, and **first-run controller guidance**. The step order, reveal and lift animation are unchanged from IP2a.

---

## What Changed in IP2b

1. **Controls (right controller only, one job per button)**
   - **A**: opens the recent Notion pages list in step 1. That is A's only job.
   - **Trigger**: select / confirm. Picks **House Sketch**, anchors the page on the table, reveals the sketch, lifts it into 3D. Pointed at the board's **Mode** button it switches Real Table / Virtual Table.
   - **Grip (hold)**: grab and move the 3D house. Releasing Grip drops it on the table.
   - **Thumbstick up/down**: while anchoring, raises or lowers the surface to match your real table.
   - **B**: reset, and recentre the workspace in front of you.

2. **Grab clarity and controller hints**
   - The house only moves while Grip is held. A **"Hold grip to grab"** badge floats above the house until the first real grab (a quick tap keeps it up).
   - A hint above the right controller shows the button for the current step and hides once the step is done.

3. **Mixed reality mode**
   - **Passthrough**: `MetaQuestPassthroughFeature` (an OpenXR feature) turns on `XR_FB_passthrough` and draws the real room behind everything the app clears to transparent.
   - **Real table anchoring**: point at your table, match the height with the thumbstick (the green outline and page preview show the surface), and pull Trigger. Reveal, lift and move then happen on that surface. There is no automatic plane detection; height is set by hand.
   - **Workspace placement**: when the headset starts tracking, the board, page list and table area appear in front of where you are facing. Press **B** to bring them in front of you again.
   - **Virtual Table fallback**: the Mode button on the left board switches to the IP2a virtual room and table.

---

## Quest Controller Mapping

| Step | Interaction | Quest input | Editor fallback |
|---|---|---|---|
| 1. Welcome | Open recent pages | **A** | `A` key |
| 2. Pick page | Select "House Sketch" | Ray + **Trigger** | Mouse aim + left click |
| 3. Anchor | Place the page on the table | Ray + **Trigger** (stick sets height) | Left click (`Up`/`Down` keys set height) |
| 4. Reveal | Show the 2D sketch | **Trigger** | Left click |
| 5. Lift | Lift the sketch into a 3D house | **Trigger** | Left click |
| 6. Move | Grab and move the house | Hold **Grip** (release drops) | Hold right mouse |
| Mode | Real Table / Virtual Table | Ray at board Mode button + **Trigger** | `V` key |
| Reset | Reset and recentre | **B** | `B` key |

---

## How to Build & Sideload onto Meta Quest

### 1. Build the APK

In Unity (`6000.3.21f1`): `Tools > DECO2300 > Build Quest APK`

The APK is written to `unity-prototype/Builds/NotionAR_Prototype2b.apk`.

### 2. Sideload onto the headset

```bash
/Applications/Unity/Hub/Editor/6000.3.21f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb install -r unity-prototype/Builds/NotionAR_Prototype2b.apk
```
