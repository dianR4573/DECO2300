# In-Class Testing — Interactive Prototype 1

## Testing process — approximately 5 minutes per participant

### 1. Intro — 30 seconds

Introduce the product without revealing too much detail. Then walk the participant through the start-up.

Suggested intro:

> This is a Unity desktop simulation of a future Meta Quest mixed-reality version of Notion. I am testing whether the interaction flow is clear and useful, not testing your ability.

### 2. Task 1: Open document selector — 30 seconds

Ask the participant to press `L` to open the document selector.

Record:

- Did they understand what appeared?
- Did the document selector feel like a menu?

### 3. Task 2: Select/place page — 45 seconds

Ask the participant to select the sketch page and place/open it on the table.

Record:

- Did the page placement make sense?
- Did the table feel like a clear workspace?

### 4. Task 3: Reveal 2D sketch — 30 seconds

Ask the participant to press `D` to reveal the 2D sketch.

Record:

- Did they understand the sketch as content inside the page?

### 5. Task 4: Lift sketch into 3D — 60 seconds

Ask the participant to press `Space` to simulate lifting the sketch into 3D.

Record:

- Did they understand the transformation?
- Did the lifting action feel connected to the 3D result?
- Did they seem interested, surprised, or confused?

### 6. Task 5: Move object — 45 seconds

Ask the participant to move the 3D object using WASD, arrow keys, mouse drag, or the implemented controls.

Record:

- Did they understand they could move the object?
- Was the movement interaction easy enough?

### 7. Post-test questions — 60–90 seconds

Ask:

1. What did you think this prototype was showing?
2. Was the table workspace understandable? Why/why not?
3. Was the 2D-to-3D transformation clear?
4. Which part was most confusing?
5. What would you expect this system to do next?
6. Rate ease of use from 1–5.
7. Rate usefulness from 1–5.

## Results logging table

| Participant | Open menu success | Place page success | Understand 2D-to-3D | Move object success | Ease 1–5 | Usefulness 1–5 | Confusion points | Improvement suggestion |
|---|---|---|---|---|---:|---:|---|---|
| P1 | Yes | Yes | Mostly yes | Yes | 4 | 4 | The participant understood the prototype, the table workspace, and the general UI flow. The main confusion was whether they could draw different objects and generate new 3D results during the test. | Make it clearer that this prototype is testing a fixed interaction flow, and future versions would support more flexible sketch input. |
| P2 | Yes | Yes | Partly | Yes | 3 | 5 | The participant understood most of the UI and workspace, but did not initially understand what was meant by the 2D-to-3D transformation until it was shown. | Add stronger visual cues, labels or an onboarding hint explaining that a flat sketch can be lifted into a 3D object with hands. THey also wanted the ability to place stuff on walls |
| P3 | Yes | Yes | Mostly yes | Yes | 4 | 5 | The participant understood the overall prototype and table workspace but some of the hand sign language was confusing. In particular the “L-shaped” hand cue was confusing with the buttons they had to press | In the next prototype there will be acutal hand tracking and the ability to use the L shaped hand feature, that should clear up confusion. |

## Summary of findings

Overall, the in class testing showed that the core concept was understandable. All three participants were able to recognise that the prototype was showing a workspace where a Notion like page could be placed on a table and used to transform a 2D sketch into a 3D object. The table workspace was consistently understood and seemed to work well as a clear visual metaphor for organising content in mixed reality.

The strongest part of the prototype was the overall UI clarity. Participants generally understood what to do and the hints on the side helped them follow the prototype without needing a long explanation. This suggests that the basic layout, document placement, and workspace structure are effective enough for Interactive Prototype 1.


## Design implications for the next prototype

The next prototype should focus on making the transformation and gesture interactions more obvious. The 2Dto3D interaction could be improved through clearer visual feedback such as labels, arrows, animation that explains the relationship between the flat sketch and the 3D object. Gesture based controls should also be represented more clearly, because participants may not understand a hand sign if they are only seeing it simulated through keyboard input.

## Notes completed during class

### Participant 1 notes

- understood what the prototype was showing.
- understood the task flow and what they were meant to do.
- understood the table workspace and saw it as a clear working area.
- Found the UI clear and easy to follow.
- Main confusion: did not know whether drawing different objects would generate different 3D results.
- This is useful feedback, but the current prototype is only testing a fixed interaction flow rather than full generative drawing.
- Ease of use rating: 4/5.
- Usefulness rating: 4/5.

### Participant 2 notes

- Mostly understood the prototype, table workspace, and basic UI flow.
- Needed more explanation for the 2D-to-3D idea.
- Initially did not understand what was meant by transforming something from 2D into 3D.
- Understood the interaction better after being shown how it worked.
- Suggested that the system should also support adding or placing objects on walls and ceilings not only the table.
- Ease of use rating: 3/5.
- Usefulness rating: 5/5.

### Participant 3 notes

- Mostly understood the prototype and the overall workspace.
- Understood the table as the main work area.
- Some of the hand sign language was confusing alongside the instructions
- This may improve in the next prototype if users can physically make the hand shape rather than only seeing it represented in the desktop simulation.
- Ease of use rating: 4/5.
- Usefulness rating: 5/5.

## Reflection

The testing helped confirm that the prototype communicates the main concept effectively, but it also showed that the most immersive parts of the design are harder to communicate in a simulated environment. The table workspace and general UI structure should be kept because participants understood them well. The 2D to 3D transformation and hand gesture interactions need clearer feedback in the next iteration especially because those interactions are a core experiance

For the next version, I would improve the prototype by adding clearer instructional cues and more visual feedback during the transformation. I would also make the gesture system more realistic by actually allowing them to simulate the hand signs. This would make the prototype feel closer to the intended experiance i want them to have and reduce confusion around keyboard inputs and hand inputs. 
