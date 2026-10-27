# Testing Plan — Interactive Prototype 2a

## Project name

**Notion AR: Turning 2D Notes into 3D Spatial Ideas**

## Three-sentence pitch

This project redesigns Notion as a mixed-reality workspace for visual note-taking and early design thinking. In Interactive Prototype 2a, users wear a Meta Quest headset and use the right controller to place a Notion-style page on a table, reveal a 2D sketch, lift it into a 3D object, and move that object in space. This test checks whether that flow is clear and useful when it runs on a real headset instead of a desktop simulation.

## What changed from Prototype 1

Prototype 1 was a desktop Unity simulation with keyboard and mouse. Prototype 2a runs on Meta Quest with head tracking and right-controller input only. The concept is the same, but the experience is now spatial and immersive, which is what this test is meant to evaluate.

## Testing objective

The main assumption I want to test is:

**Can users understand and complete the Notion AR flow on Quest using only the right controller, and do they still find the 2D-to-3D idea useful in headset?**

I also want to check:

1. Whether the table feels like a clear workspace in VR.
2. Whether opening the document picker and placing a page makes sense with ray + trigger.
3. Whether revealing the sketch and lifting it into 3D feels connected and useful.
4. Whether moving the 3D object with grip / ray-drag is comfortable.
5. Where people hesitate or need help.

## Testing methodology

This is a short task-based usability walkthrough on Meta Quest.

- About **5 minutes** per participant.
- Aim for **at least 5** peers in the week 9 studio (more if there is time).
- Same task order for everyone so results can be compared.
- I observe, note success/fail, ask short questions, and log ratings.
- Someone else should be able to run this plan from this document alone.

## Prototype description

The Quest build includes:

- A 1:1 scale table workspace.
- A floating Notion-style document picker.
- A page that can be aimed and placed on the table.
- A 2D house sketch on the page.
- A lift animation that turns the sketch into a simple 3D house.
- Movement of the 3D house across the table.
- An in-room instruction board that shows the current step only.
- Facilitator reset with the **B** button between participants.

Out of scope for this prototype:

- Real Notion login or sync.
- Real passthrough AR.
- Hand tracking / L-shaped hand gesture (controller stands in for now).
- Freehand drawing recognition or auto 3D generation from any sketch.

## Controls (right controller only)

| Step | What the user does | Input |
|---|---|---|
| 1 | Open recent Notion pages | **A** button |
| 2 | Select House Sketch card | Ray aim + **Trigger** |
| 3 | Place page on table | Ray aim at table + **Trigger** |
| 4 | Reveal 2D sketch | **A** button |
| 5 | Lift sketch into 3D | **Trigger** |
| 6 | Move 3D house | Hold **Grip** (or ray-drag) |
| Reset (facilitator) | Reset between people | **B** button |

## Setup before class

1. Build / sideload `NotionAR_Prototype2a.apk` onto the Quest.
2. Confirm Developer Mode / Unknown Sources can launch the app.
3. Print or keep this plan + the results table on a phone or laptop.
4. Have sanitising wipes for the headset.
5. Do one full dry run yourself before the first participant.

Needed materials:

- Meta Quest with the prototype installed.
- This testing plan.
- Results table (below).
- Timer.
- Optional: spare controller battery / charging cable.

## Before each participant

1. Reset with **B**.
2. Check the instruction board shows the first step.
3. Tell them this is a concept prototype, not a finished product.
4. Ask them to think aloud if something feels confusing.
5. Help with headset fit only. Do not explain the full button map unless they are stuck for more than about 10 seconds.

## Intro script (about 30 seconds)

> This is a Meta Quest prototype of a future Notion AR workspace. You will open a page, put it on the table, turn a 2D sketch into a 3D object, and move it. I am testing whether the interaction makes sense on the headset, not whether the app looks finished. Use the right controller. The board in front of you will show the current step.

## Tasks (about 5 minutes total)

### Task 1 — Open document picker (30 sec)

Ask them to press **A** to call recent Notion pages.

Record:

- Success / fail / needed help
- Did they understand what appeared?

### Task 2 — Select and place page (45–60 sec)

Ask them to aim at **House Sketch**, pull **Trigger** to select, then aim at the table and pull **Trigger** to place it.

Record:

- Success / fail / needed help
- Did the table feel like a clear workspace?

### Task 3 — Reveal 2D sketch (30 sec)

Ask them to press **A** to reveal the sketch on the page.

Record:

- Success / fail / needed help
- Did they see the sketch as content on the page?

### Task 4 — Lift into 3D (45–60 sec)

Ask them to pull **Trigger** to lift the sketch into a 3D house.

Record:

- Success / fail / needed help
- Did the lift feel connected to the 3D result?
- Interested / surprised / confused?

### Task 5 — Move the 3D object (45 sec)

Ask them to move the house using **Grip** (or ray-drag).

Record:

- Success / fail / needed help
- Was movement comfortable?

### Post-test questions (60–90 sec)

Ask these after they take the headset off:

1. What did you think this prototype was showing?
2. Was the table workspace clear in the headset? Why / why not?
3. Was the 2D-to-3D step clear and useful?
4. Which part was most confusing?
5. What would you expect this system to do next?
6. Ease of use: 1–5
7. Usefulness: 1–5
8. One thing to improve for the next version

## Results table

Fill this during / right after each test.

| P# | Open menu | Place page | Reveal sketch | Lift 2D→3D | Move object | Ease 1–5 | Useful 1–5 | Confusion | Improvement |
|---|---|---|---|---|---|---:|---:|---|---|
| P1 |  |  |  |  |  |  |  |  |  |
| P2 |  |  |  |  |  |  |  |  |  |
| P3 |  |  |  |  |  |  |  |  |  |
| P4 |  |  |  |  |  |  |  |  |  |
| P5 |  |  |  |  |  |  |  |  |  |

Use **Yes / Mostly / No** for the task columns, and note if they needed a hint.

## What counts as success

The session goes well if most participants can:

- Complete the full loop on Quest without major rescue.
- Understand the table as the workspace.
- Understand that the sketch becomes a 3D object.
- Say whether the idea feels useful or not (even a “no” is useful data).
- Point to at least one clear improvement for IP2b.

## After class

1. Copy filled results into `docs/IP2a-testing-results.md` (or update this file).
2. Write a short summary:
   - What worked
   - Common confusion points
   - What to change for IP2b
   - Whether 2D-to-3D should stay the centre of the concept
3. Submit the testing plan and results as required on Blackboard.
4. Push the docs to GitHub when you can.

## Notes for whoever runs the test

- Keep each run to ~5 minutes so you can test more people.
- Do not over-explain. Let the instruction board and the tasks do the work.
- If the headset loses tracking or the app crashes, reset, note it, and restart that participant if there is time.
- Comfort first: if someone feels dizzy or uncomfortable, stop the test.
