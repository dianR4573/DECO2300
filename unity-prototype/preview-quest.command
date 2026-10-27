#!/bin/bash
# Shows a live view of what the person wearing the Quest sees (left eye), in a window on this Mac.
# Double-click this file in Finder, or run it from the terminal. Close the window to stop.
# Needs ffmpeg (brew install ffmpeg).

ADB="/Applications/Unity/Hub/Editor/6000.3.21f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
FFPLAY="$(command -v ffplay || echo /opt/homebrew/bin/ffplay)"
PIPE="$(mktemp -u)"
RECORDER=""

stop_recorder() {
  if [ -n "$RECORDER" ]; then
    pkill -P "$RECORDER" 2>/dev/null
    kill "$RECORDER" 2>/dev/null
    RECORDER=""
  fi
  "$ADB" shell pkill screenrecord 2>/dev/null
  rm -f "$PIPE"
}
trap stop_recorder EXIT INT TERM

echo "Looking for the headset..."
until [ "$("$ADB" get-state 2>/dev/null)" = "device" ]; do
  echo "  Not ready. Plug in the Quest, put it on and accept the USB debugging prompt."
  sleep 3
done

echo "Live view starting. The window appears once the headset sends its first picture,"
echo "which only happens while someone is wearing it. Close the window to stop."

while true; do
  mkfifo "$PIPE"

  # screenrecord stops after 3 minutes, so keep restarting it in the background.
  ( while true; do
      "$ADB" exec-out screenrecord --output-format=h264 --size 1832x960 --bit-rate 8000000 -
      sleep 0.2
    done ) > "$PIPE" &
  RECORDER=$!
  disown "$RECORDER"   # no "Terminated" message when it is stopped

  # -alwaysontop keeps the window in front of Terminal so it can't open hidden behind it.
  "$FFPLAY" -loglevel error -window_title "Quest live view" -alwaysontop -an \
    -fflags nobuffer -flags low_delay -framedrop -probesize 32 -sync ext \
    -vf "crop=iw/2:ih:0:0" -i "$PIPE"
  STATUS=$?
  stop_recorder

  # Closing the window exits cleanly (0); anything else is a dropped stream, so try again.
  [ "$STATUS" -eq 0 ] && break
  echo "  Live view stopped unexpectedly. Restarting..."
  sleep 2
done
