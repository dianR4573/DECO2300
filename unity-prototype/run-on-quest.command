#!/bin/bash
# Installs the latest Notion AR build on the plugged-in Quest and launches it.
# Double-click this file in Finder, or run it from the terminal.

ADB="/Applications/Unity/Hub/Editor/6000.3.21f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
APK="$(dirname "$0")/Builds/NotionAR_Prototype2b.apk"
APP="com.deco2300.notionar"

echo "Looking for the headset..."
until [ "$("$ADB" get-state 2>/dev/null)" = "device" ]; do
  echo "  Not ready. Plug in the Quest, put it on and accept the USB debugging prompt."
  sleep 3
done

echo "Installing $(basename "$APK")..."
"$ADB" install -r "$APK" || { echo "Install failed."; exit 1; }

echo "Launching Notion AR..."
"$ADB" shell am force-stop "$APP"
"$ADB" shell monkey -p "$APP" -c android.intent.category.LAUNCHER 1 > /dev/null 2>&1

echo "Done. Put the headset on and press B to recentre."
echo
echo "Watching for the 'Boundary not found' popup for 2 minutes (it steals the controller, so you get no ray)."
echo "Press Ctrl+C to stop watching."
# The boundary popup runs as com.oculus.os.clearactivity. Other screens flash up briefly while the
# app launches, so only warn when the popup is still on top two checks in a row.
SEEN=0
for i in $(seq 1 40); do
  TOP=$("$ADB" shell dumpsys activity activities | grep -m1 topResumedActivity)
  if [[ "$TOP" == *clearactivity* ]]; then
    SEEN=$((SEEN + 1))
  else
    SEEN=0
  fi

  if [ "$SEEN" -ge 2 ]; then
    echo "  The headset is showing the 'Boundary not found' popup."
    echo "  In the headset choose 'Continue in Passthrough' (or create a boundary)."
    exit 0
  fi
  sleep 3
done
echo "  No popup. You're good to go."
