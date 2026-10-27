# Sideload the IP2a APK onto Meta Quest

## What this means

Sideloading = install your built `.apk` onto the Quest from a file, not from the Quest Store.

## Steps

### 1. Build the APK

1. Open `unity-prototype` in Unity.
2. Open the main Notion AR scene.
3. Use `Tools > DECO2300 > Build Quest APK`  
   or `File > Build Settings...` → **Android** → **Build**.
4. Save as `unity-prototype/Builds/NotionAR_Prototype2a.apk`.

### 2. Turn on Developer Mode

On your phone: Meta Quest app → headset → **Developer Mode** on.

### 3. Install the APK

Plug Quest into your Mac with USB-C. Accept the allow prompt in the headset.

Then do one of these:

- **SideQuest:** connect headset → drag the `.apk` in → wait for install.
- **Meta Quest Developer Hub:** connect headset → install the `.apk`.
- **Terminal (using Unity's bundled ADB):**  
  ```bash
  /Applications/Unity/Hub/Editor/6000.3.21f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb install -r unity-prototype/Builds/NotionAR_Prototype2a.apk
  ```
  *(Or if `adb` is in your PATH: `adb install -r unity-prototype/Builds/NotionAR_Prototype2a.apk`)*

### 4. Open it on the Quest

App Library → **Unknown Sources** → launch **Notion AR**.

### 5. Dry run once

**A** → select/place → reveal → lift → move → **B** reset.
