# AR Geometry Study Guide — Starter Kit

Unity + Vuforia starter for the CAP4111 final project. Gets you to a working
**Shape Viewer** (scan a card or pick a shape → 2D/3D model → live measurements)
and **Quiz** screen, matching the proposal sketches.

## What's in `Scripts/`

| File | Role | Proposal area |
|---|---|---|
| `Geometry.cs` | All formulas, names, units, text. Pure C#, no Unity objects. | Dimensions & formulas |
| `ShapeController.cs` | Builds the model from primitives, 2D/3D toggle, resize, touch-drag rotation (ignores drags that start on UI). | AR visualization |
| `ShapeCardTarget.cs` | Put on each ImageTarget; when a card is detected the model jumps onto it. | AR recognition |
| `ShapeViewerUI.cs` | Info panel, 2D/3D buttons, dimension sliders, shape menu (manual fallback). | Viewer UI |
| `QuizManager.cs` | Generates questions in-app (calculate / pick formula / identify pairing), feedback, score. | Study & assessment |

Shape pairings (explicit, per the proposal's "Matching 2D and 3D" fix):
Square ↔ Cube, Circle ↔ Sphere, Rectangle ↔ Rectangular Prism. Add more by
extending the `ShapeKind` enum and the switch statements in `Geometry.cs` and
`ShapeController.ApplyScale`.

## Setup (one person does this, then push)

1. **New Unity project** (3D, same Unity version for everyone). Add the
   [Unity .gitignore](https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore) before the first commit.
2. **Vuforia**: import from the Asset Store, Window → Vuforia Configuration →
   paste the license key. Delete Main Camera, add AR Camera.
   Set *Max Simultaneous Tracked Images* to 1 (one card at a time keeps things simple).
3. **TextMeshPro**: Window → TextMeshPro → Import TMP Essential Resources.
4. **Input System**: the scripts use the new Input System (same as the Rotor
   script from the animations lecture). If prompted, enable it and restart.
5. Copy `Scripts/` into `Assets/Scripts/`.

## Notecards (image targets)

Plain line drawings of a square or circle will track badly — FAST/ORB need
corners and texture, and a single symmetric shape has neither. Design each card as:

- the shape drawn large in the middle, **plus**
- a busy, non-repeating border or background (photo collage, noise texture,
  scattered icons), different per card, high contrast.

Upload each to Target Manager (Device database), check for 4–5 stars, measure the
**printed** width in meters, download for Unity Editor, double-click to import.
Print at 100% scale.

## Scene wiring

```
AR Camera
  └─ ManualAnchor          (empty, local pos 0,0,0.3)  ← shapes picked from the menu appear here
ImageTarget_Square         + ShapeCardTarget (shape = Square)
ImageTarget_Circle         + ShapeCardTarget (shape = Circle)
ImageTarget_Rectangle      + ShapeCardTarget (shape = Rectangle)
ShapeAnchor                + ShapeController (assign a lit material)
Canvas (Screen Space - Overlay, Scale With Screen Size, 1080x1920)
  ├─ ViewerPanel           + ShapeViewerUI
  │    InfoText, 2D button, 3D button, SliderA/B/C (min 1, max 10),
  │    "Choose / Insert Shape", "Scan Notecard", "Start Quiz"
  ├─ ShapeMenuPanel        Square / Circle / Rectangle buttons → PickSquare/PickCircle/PickRectangle
  └─ QuizPanel             + QuizManager
       ProgressText, PromptText, 3 option buttons, Check button, FeedbackText, ScoreText,
       "Return to Shape Viewer" → QuizManager.ReturnToViewer
EventSystem                (replace StandaloneInputModule with InputSystemUIInputModule if asked)
```

Button hooks: "Start Quiz" → `ShapeViewerUI.ShowQuiz`; "Choose / Insert Shape" →
`ShapeViewerUI.OpenShapeMenu`; "Scan Notecard" can just hide the menu / show a
"point at a card" hint — detection is always on.

## Suggested split (4 people, the 3 proposal areas + integration)

| Person | Owns | First milestone |
|---|---|---|
| A | Cards + Vuforia: design/test targets, `ShapeCardTarget`, phone build (Android/iOS) | 3 cards at 4+ stars, model appears on each card on a phone |
| B | `ShapeController` + touch: rotation feel, pinch-to-scale (optional), 2D/3D visuals, dimension labels in 3D | Rotate + toggle works on phone without fighting the sliders |
| C | `Geometry` + `ShapeViewerUI`: canvas layout matching the sketch, more shapes (cylinder, triangle/prism) | Sliders update numbers live for all shapes |
| D | `QuizManager`: quiz UI, more question types, end screen; OCR stretch goal later | 5-question quiz playable start to finish |

Rough order: (1) everyone builds the empty scene from this README and confirms
it runs in the editor with a webcam; (2) each person works in their own
prefab/script to avoid scene merge conflicts — only one person edits the main
scene at a time; (3) integrate, then phone testing; (4) stretch: OCR, ground plane.

## Known gaps / TODO

- Not yet compiled inside Unity — expect a few small fixes (Vuforia API names
  change between versions; check the "Status" enum against your version).
- 3D dimension labels on the model (e.g. "4 cm" along an edge) aren't drawn yet —
  good task for person B (world-space TextMeshPro, see the labeling lecture
  on keeping labels near their object and avoiding overlap).
- Pinch-to-resize is not implemented; sliders cover resizing for now.
- OCR keyword recognition is out of scope for the starter (proposal lists it as
  "if time permits").
