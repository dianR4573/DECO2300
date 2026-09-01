# Interactive Prototype Outcome Evaluation 1

## Objective and validation metrics

For this evaluation, I wanted to understand whether people would actually see value in using a mixed-reality Notion-style workspace where 2D page content can be placed in the room and transformed into a 3D object. I was also testing whether the interaction flow I implemented felt instinctive to users, or whether it mainly made sense because I designed it from a developer mindset.

My testing focused on five areas:

| Testing aim | Success metric |
|---|---|
| Understandability of the concept | Participants can explain that the prototype shows a Notion-like page/workspace where 2D content becomes a 3D object. |
| Clarity of the table workspace | Participants understand the table as the main working area for placing and interacting with page content. |
| Clarity of the 2D-to-3D transformation | Participants understand that a flat sketch can be lifted or converted into a 3D object. |
| Usefulness and possible use cases | Participants can imagine how they might use this feature or suggest future uses. |
| Gesture and document picker interaction | Participants can understand the simulated `L`/hand gesture idea, or identify where the interaction should be improved. |

I defined success as participants being able to complete the main flow with limited explanation, describe what the prototype was trying to communicate, and give useful feedback about what felt clear, confusing, or worth developing further.

## Results

Three participants completed the in-class testing process. Each participant was asked to open the document selector, place a page on the table, reveal a 2D sketch, transform the sketch into a 3D object, move the object, and answer short post-test questions.

| Participant | Open menu success | Place page success | Understood 2D-to-3D | Move object success | Ease of use | Usefulness | Key confusion point | Improvement suggestion |
|---|---|---|---|---|---:|---:|---|---|
| P1 | Yes | Yes | Mostly yes | Yes | 4/5 | 4/5 | Understood the prototype, table workspace, and UI flow, but was unsure whether users could draw different objects and generate different 3D results. | Clarify that this prototype tests a fixed interaction flow, while future versions would support more flexible sketch input. |
| P2 | Yes | Yes | Partly | Yes | 3/5 | 5/5 | Understood the UI and workspace, but did not initially understand the 2D-to-3D transformation until it was shown. | Add clearer visual cues, labels, onboarding, and consider supporting object placement on walls as well as tables. |
| P3 | Yes | Yes | Mostly yes | Yes | 4/5 | 5/5 | Understood the overall prototype and table workspace, but found some of the hand sign language confusing, especially the L-shaped cue beside keyboard instructions. | Test the hand gesture more realistically with actual hand tracking so the intended interaction is clearer. |

Overall, all three participants could complete the core interaction sequence, and all participants understood the table as a workspace. The ease-of-use scores were mostly positive, with two participants rating it 4/5 and one participant rating it 3/5. Usefulness was also strong, with two participants rating it 5/5 and one participant rating it 4/5.

## Analysis and insights

The results show that the core concept is understandable and worth continuing. Participants generally recognised that the prototype was demonstrating a spatial workspace where a Notion-style document could become more interactive through mixed reality. This validates the basic idea of using a table as a familiar anchor for digital content, because participants understood it without needing a long explanation.

A key pattern was that the workspace and document placement were clearer than the transformation and gesture interactions. The table metaphor worked because it gave users a recognisable place to organise and interact with content. In contrast, the 2D-to-3D transformation required more support, especially for P2, who only understood it properly after seeing it demonstrated. This suggests that the transformation is interesting, but it needs stronger visual feedback so users understand what is happening before and during the interaction.

Another important insight is that users expected the system to be more flexible than the current prototype. P1 wanted to know whether different drawings could create different 3D outputs, and P2 suggested placing objects on walls as well as on the table. This shows that users were not only following the current task; they were also imagining broader use cases. That is valuable because it suggests the concept has potential beyond the fixed interaction flow I implemented for the prototype.

The testing also showed a limitation in how the interaction was prototyped. Because this version was a desktop Unity simulation, users had to pretend they were using a mixed-reality headset. This made the gesture interaction harder to judge. The L-shaped hand/document picker idea may still work, but the test did not fully validate it because participants were using keyboard input rather than actual hand tracking.

## Evaluation of aims

The testing mostly validated the overall concept and workspace design. Participants understood the basic purpose of the prototype and were able to complete the main flow. This suggests that the idea of a mixed-reality Notion workspace has enough clarity and usefulness to continue developing.

The table workspace aim was strongly validated. All participants understood the table as the main working area, and there was no major confusion about placing the page in the workspace. This means the table should remain part of the next iteration because it gives the interface a clear physical structure.

The 2D-to-3D transformation aim was partially validated. Participants were interested in the transformation, but it was not always immediately obvious what was happening. This means the interaction is promising, but the next prototype needs clearer onboarding, labels, animation, or visual cause-and-effect feedback.

The gesture/document picker aim is still uncertain. The L-shaped hand idea created some confusion when represented through desktop controls. This does not necessarily mean the gesture is wrong, but it does mean I need to test it in a more realistic mixed-reality prototype before deciding whether it should remain the main document picker interaction.

The usefulness aim was validated. The usefulness scores were high, and participants suggested future behaviours such as generating multiple 3D shapes and placing objects in different parts of the room. This indicates that users could imagine practical or creative uses for the feature.

## Concept iteration

Based on the evaluation, my next concept iteration will keep the table-based workspace but make the prototype closer to the intended mixed-reality experience. The biggest change is moving from a desktop simulation toward a live working AR/MR prototype that a user can experience in a headset. This is important because the interaction depends on spatial presence, hand movement, and physical context, which are difficult to evaluate properly on a desktop.

The next prototype should include:

1. **A real headset-based workflow** where the user can complete one core task from start to finish in AR/MR rather than only simulating the experience on desktop.
2. **Support for multiple drawable shapes**, so users can draw different 2D forms and see them become different 3D objects instead of only testing one fixed example.
3. **Clearer transformation feedback**, such as animation, arrows, labels, progress cues, or a visual link between the 2D sketch and the generated 3D object.
4. **Improved document picker interaction**, including more realistic hand tracking and further testing of whether the L-shaped hand gesture feels natural or if a more creative interaction would be better.
5. **Expanded spatial placement**, including the possibility of placing content or objects on walls, not only on the table.
6. **More focused UI feedback testing**, because the current testing showed that users understood the broad concept, but the next version needs to test the detailed interface and interaction choices.

These changes respond directly to the testing results. I am keeping the parts that worked, especially the workspace structure, while improving the areas that caused confusion: transformation clarity, gesture input, and the limits of the fixed desktop prototype.

## Reflection and future testing

This evaluation helped me understand that the simulator worked for communicating the general idea, but it did not fully test the intended AR/MR experience. Users mostly understood what the demo was doing, and the confusion that appeared was mainly around interaction details rather than the whole concept. This tells me that the concept is understandable, but the next prototype needs to give users a more realistic way to experience it.

The main limitation of my method was that I tested an AR project through desktop interaction. Participants had to imagine the headset experience instead of actually using it. This made it harder to evaluate whether the hand gestures and spatial interactions would feel natural. A mixed-reality headset prototype would be much stronger because it would let users experience the system in the same context I am designing for.

I also learned that I need to be careful not to design only from a developer mindset. The current prototype made sense as a controlled interaction flow, but users expected more flexibility, such as drawing multiple shapes and placing objects in different parts of the room. Their feedback showed me that the concept should not only demonstrate a technical transformation; it should support a useful creative workflow.

For the next round of testing, I want to test ease of use with a real working headset prototype. I would ask users to complete a core workflow, observe whether they understand the UI without explanation, and gather feedback on the document picker, hand gestures, transformation feedback, and additional features they would want. This will help me evaluate whether the concept is not only understandable, but also usable and valuable in the intended mixed-reality environment.

## Appendix A: Raw participant notes

### Participant 1

- Understood what the prototype was showing.
- Understood the task flow and what they were meant to do.
- Understood the table workspace and saw it as a clear working area.
- Found the UI clear and easy to follow.
- Main confusion: did not know whether drawing different objects would generate different 3D results.
- This is useful feedback, but the current prototype is only testing a fixed interaction flow rather than full generative drawing.
- Ease of use rating: 4/5.
- Usefulness rating: 4/5.

### Participant 2

- Mostly understood the prototype, table workspace, and basic UI flow.
- Needed more explanation for the 2D-to-3D idea.
- Initially did not understand what was meant by transforming something from 2D into 3D.
- Understood the interaction better after being shown how it worked.
- Suggested that the system should also support adding or placing objects on walls and ceilings, not only the table.
- Ease of use rating: 3/5.
- Usefulness rating: 5/5.

### Participant 3

- Mostly understood the prototype and the overall workspace.
- Understood the table as the main work area.
- Some of the hand sign language was confusing alongside the keyboard instructions.
- This may improve in the next prototype if users can physically make the hand shape rather than only seeing it represented in the desktop simulation.
- Ease of use rating: 4/5.
- Usefulness rating: 5/5.
