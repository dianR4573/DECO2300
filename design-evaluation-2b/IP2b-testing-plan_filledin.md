# IP2b Testing Plan

## Project
**Notion AR: Turning 2D Notes into 3D Spatial Ideas**

## What are we testing?

In Evaluation 2, people placed the page on the table easily, but the controls didn't work well. All 3 testers tapped the grip instead of holding it, and the buttons felt random.

IP2b tests two things:

1. **Mixed reality:** can users place the page on their real table (passthrough) and use it there?
2. **Fixed controls:** do the new button layout and the hold-to-grab hint fix the problems from Evaluation 2?

### Core flow
**Open pages → Select House Sketch → Place on real table → Reveal sketch → Turn into 3D → Move house**

## Controls

- **Trigger** = select / place / lift
- **Grip (hold)** = grab / move
- **A** = open pages
- **B** = reset (facilitator)

## Test setup

- Meta Quest 3S with IP2b installed and charged
- Right controller
- A real table with clear space
- Results sheet
- Reset with face forward + **B** before each participant (setup step to set the table and height)
- Aim for **5 participants** plus the tutor — tested **4** (P1, P2, P3 tutor, P4); P5 not tested
- About 5 minutes per participant
- Tested Thu 22 Oct 2026: 2 in the Week 12 studio and 2 at home
- Before testing, the facilitator briefly explained what the prototype is for and that the page goes on the table

## Test tasks

### 1. Open pages
Ask:
> Start when you're ready.

Watch whether they open the page picker and they are able to read all the text.

### 2. Select and place
Ask:
> Select the House Sketch and put it on the real table.

Watch whether it sits on the table and stays put.

### 3. Reveal the sketch
Ask:
> Show me what is on the page.

see if they are able to understand and reaveal the sketch on the page.

### 4. Turn it into 3D
Ask:
> Turn the sketch into a 3D object.

see if they are able to click the trigger and turn the sketch into a 3d object.

### 5. Move the house
Ask:
> Move the house around the table.

Watch whether they **hold** the grip without a hint. see if its instinctive

## What to record

- **Yes** = completed independently
- **Mostly** = completed with a hint
- **No** = could not complete

| Participant | Open | Place | Reveal | 2D→3D | Move | Ease 1–5 | Useful 1–5 | Main confusion / note |
|---|---|---|---|---|---|---:|---:|---|
| P1 (returning from 2a) | Yes | Yes | Yes | Yes | Yes | 4.9 | 4.9 | Flew through, often without facilitator saying anything |
| P2 (returning from 2a) | Yes | Yes | Yes | Yes | Yes | 4.8 | 4.9 | Flew through |
| P3 (tutor, new) | Yes | Mostly | Yes | Yes | Yes | 4.5 | 4.8 | Placed controller on table but kept pressing buttons while flat, revealed sketch early; height / controller-on-table placement unclear; facilitator stepped in |
| P4 (new) | Yes | Mostly | Yes | Yes | Yes | 4.3 | 4.7 | Put controller down flat and left it instead of pressing; wrong height; facilitator stepped in |
| P5 | not tested | | | | | | | |
| Tutor | = P3 | | | | | | | |

Averages: Ease **4.63**/5 · Usefulness **4.83**/5 (Evaluation 2: 4.1 and 3.9). All four opened the picker with A, read all text, used Trigger to reveal and turn into 3D, and moved the house by holding Grip without a hint (0 of 3 in Evaluation 2).

## After the test

Ask:

1. What did you think this prototype was showing?
2. Did the real table help, or was it harder than the virtual one?
3. Did the buttons do what you expected?
4. What was the most confusing part?
5. What would you improve?

Then record:

- **Ease:** 1–5
- **Usefulness:** 1–5

### Answers (all four participants)

1. **What it showed:** All knew. The facilitator briefly explained the design and what it mimics before testing.
2. **Real table:** A bit more of a challenge than the virtual table (the virtual table was very easy to identify; real tables can be messy). Users still found it easily and placed the item. They understood it belonged on the table, and the facilitator told them beforehand that they needed to place it there.
3. **Buttons:** Performed as expected.
4. **Most confusing:** Table placement (guessing how to place it).
5. **Improve:** Visual instructions showing how to place the item correctly on the table.

## Five design principles

The five XR design principles. During each test I will watch for the evidence in the third column. After all participants, write a short verdict for each principle.

| Principle | How IP2b uses it | What to watch for in testing | Verdict |
|---|---|---|---|
| **1. Text readability** | Page picker, page text and board instructions are shown in the headset at a fixed size near the table. | Can participants read every label and instruction without leaning in or asking? | **Met.** Everyone could read all text. |
| **2. Ergonomics** | The page is anchored on a real table, its height can be adjusted (about 0.45–1.15 m), and the house stays within about 0.55 m of the page so everything is within arm's reach. B recentres the workspace in front of the user. | Do they stretch, bend or turn awkwardly to reach or see anything? | **Met.** Nobody stretched, bent or turned awkwardly. Nobody adjusted table height with the thumbstick. |
| **3. Brightness changes** | Passthrough shows the real room, with a switch between the real table and a virtual table. | Does the user need to switch becuase the passthough mode is not working? | **Met.** Everyone used AR passthrough the whole time. Nobody switched to the virtual table. |
| **4. Avoiding simulator sickness** | Passthrough keeps the real room visible, and the house lift is a smooth eased animation. | Any dizziness, eye strain or discomfort? | **Met.** Nobody reported dizziness or eye strain. |
| **5. Button placement** | One job per button: Trigger selects, hold Grip grabs, A opens pages, B resets. A hint on the controller shows "Hold grip to grab". | Do they press the right button first time? Fewer wrong presses than Evaluation 2? | **Met.** People pressed the right buttons. All four moved the house by holding Grip without a hint (0 of 3 in Evaluation 2). P3 pressed extra buttons during table placement. |

## What are we looking for(results wise)

After 5 participants, identify:

- Whether people can move the house without a hint (0 of 3 could in Evaluation 2)
- Whether the real table works as a workspace
- Whether the new controls make sense
- The main change to make next

### Findings from this session (4 participants)

- All four moved the house without a hint.
- The real table worked as a workspace once placed, but was harder to discover than the virtual table. New users (P3, P4) needed help with controller-on-table placement.
- The new controls made sense. Buttons performed as expected.
- Main change next: a visual guide for placing the page / controller on the real table, including when to press while flat.

## Complains from previous evaluation

- **Evaluation 2 finding:** grip was tapped instead of held, and the buttons felt random.

- **Week 12 result:** The controls are fixed and everyone moved the house without a hint, but new users need a visual guide for placing it on the real table.
