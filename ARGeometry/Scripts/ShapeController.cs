using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Owns the one virtual model on screen. Everything else (UI, card handlers,
// quiz) talks to this. Attach to an empty GameObject named "ShapeAnchor".
//
// The model is built from Unity primitives, so no imported assets are needed:
//   Square    -> 2D: very thin Cube         3D: Cube
//   Circle    -> 2D: very thin Cylinder     3D: Sphere
//   Rectangle -> 2D: very thin Cube         3D: Cube scaled w x h x l
public class ShapeController : MonoBehaviour
{
    public static ShapeController Instance { get; private set; }

    [Header("Current state")]
    public ShapeKind kind = ShapeKind.Square;
    public ViewMode mode = ViewMode.ThreeD;
    public ShapeDims dims = new ShapeDims();

    [Header("Display")]
    [Tooltip("Unity meters per real-world cm of the *math* value. 0.01 = true scale (4 cm cube is 4 cm). " +
             "Drop this if models get bigger than the card.")]
    public float metersPerCm = 0.01f;
    [Tooltip("Thickness of 2D shapes, in meters.")]
    public float flatThickness = 0.001f;
    public Material shapeMaterial;

    [Header("Touch rotation")]
    public float degreesPerPixel = 0.3f;

    // Fires whenever kind/mode/dims change, so the UI can refresh.
    public event System.Action Changed;

    private GameObject model;      // the primitive currently shown
    private Transform pivot;       // rotated by touch; model sits under it

    void Awake()
    {
        Instance = this;
        pivot = new GameObject("Pivot").transform;
        pivot.SetParent(transform, false);
        Rebuild();
    }

    // ---------- public API ----------

    public void SetShape(ShapeKind k)       { kind = k; Rebuild(); }
    public void SetMode(ViewMode m)         { mode = m; Rebuild(); }
    public void ToggleMode()                { SetMode(mode == ViewMode.TwoD ? ViewMode.ThreeD : ViewMode.TwoD); }
    public void SetDimension(int index, float cm)
    {
        cm = Mathf.Max(0.1f, cm);
        if (index == 0) dims.a = cm; else if (index == 1) dims.b = cm; else dims.c = cm;
        ApplyScale();
        Changed?.Invoke();
    }
    public void ResetRotation() { pivot.localRotation = Quaternion.identity; }

    // Move the whole model under a new parent (an ImageTarget, or a ground
    // plane anchor) so it gets registered to that target.
    public void AttachTo(Transform parent)
    {
        transform.SetParent(parent, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        // Undo any target scale so cm stay cm.
        Vector3 s = parent != null ? parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
    }

    // ---------- building ----------

    void Rebuild()
    {
        if (model != null) Destroy(model);

        PrimitiveType p =
            kind == ShapeKind.Circle
                ? (mode == ViewMode.TwoD ? PrimitiveType.Cylinder : PrimitiveType.Sphere)
                : PrimitiveType.Cube;

        model = GameObject.CreatePrimitive(p);
        model.name = Geometry.Name(kind, mode);
        Destroy(model.GetComponent<Collider>());   // we rotate by screen drag, no raycasts needed
        model.transform.SetParent(pivot, false);
        if (shapeMaterial != null) model.GetComponent<Renderer>().sharedMaterial = shapeMaterial;

        ApplyScale();
        Changed?.Invoke();
    }

    void ApplyScale()
    {
        if (model == null) return;
        float k = metersPerCm, t = flatThickness;
        Vector3 s;
        switch (kind)
        {
            case ShapeKind.Square:
                s = mode == ViewMode.TwoD ? new Vector3(dims.a * k, t, dims.a * k)
                                          : Vector3.one * dims.a * k;
                break;
            case ShapeKind.Circle:
                float d = 2 * dims.a * k;   // primitives are 1 unit across -> diameter
                // Cylinder primitive is 2 units tall, so halve its Y.
                s = mode == ViewMode.TwoD ? new Vector3(d, t * 0.5f, d) : Vector3.one * d;
                break;
            default:
                s = mode == ViewMode.TwoD ? new Vector3(dims.a * k, t, dims.b * k)
                                          : new Vector3(dims.a * k, dims.c * k, dims.b * k);
                break;
        }
        model.transform.localScale = s;
        // Sit 3D solids on top of the card instead of half inside it.
        model.transform.localPosition = new Vector3(0, mode == ViewMode.TwoD ? t : s.y * 0.5f, 0);
    }

    // ---------- touch / mouse rotation ----------
    // One-finger drag rotates. Drags that start on UI (sliders, buttons) are
    // ignored, which is the "keep manipulation separate from UI" fix from the proposal.

    private bool dragging;

    void Update()
    {
        Vector2 delta = Vector2.zero;
        bool pressed = false, began = false;
        Vector2 pos = Vector2.zero;
        int pointerId = -1;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var t = Touchscreen.current.primaryTouch;
            pressed = true;
            began = t.press.wasPressedThisFrame;
            pos = t.position.ReadValue();
            delta = t.delta.ReadValue();
            pointerId = t.touchId.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)   // editor testing
        {
            pressed = true;
            began = Mouse.current.leftButton.wasPressedThisFrame;
            pos = Mouse.current.position.ReadValue();
            delta = Mouse.current.delta.ReadValue();
        }

        if (!pressed) { dragging = false; return; }
        if (began) dragging = !PointerOverUI(pointerId);
        if (!dragging) return;

        // Horizontal drag spins about the up axis, vertical drag tilts.
        pivot.Rotate(Vector3.up, -delta.x * degreesPerPixel, Space.Self);
        pivot.Rotate(Vector3.right, delta.y * degreesPerPixel, Space.World);
    }

    static bool PointerOverUI(int touchId)
    {
        if (EventSystem.current == null) return false;
        return touchId >= 0 ? EventSystem.current.IsPointerOverGameObject(touchId)
                            : EventSystem.current.IsPointerOverGameObject();
    }
}
