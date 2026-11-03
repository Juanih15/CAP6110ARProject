using UnityEngine;
using Vuforia;

// Attach to each ImageTarget (one per printed notecard). Set "shape" in the
// Inspector. When the card is found, the shared model moves onto this card and
// switches to that shape. Same callback pattern as the 9-15 Vuforia-Tracking slides.
public class ShapeCardTarget : MonoBehaviour
{
    public ShapeKind shape = ShapeKind.Square;

    private ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null) observer.OnTargetStatusChanged += OnStatusChanged;
    }

    void OnDestroy()
    {
        if (observer != null) observer.OnTargetStatusChanged -= OnStatusChanged;
    }

    void OnStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        // Only react to a real detection, not to EXTENDED_TRACKED keeping it alive.
        if (status.Status != Status.TRACKED) return;

        var sc = ShapeController.Instance;
        if (sc == null) return;
        if (sc.transform.parent == transform && sc.kind == shape) return;   // already here

        sc.AttachTo(transform);
        sc.SetShape(shape);
        Debug.Log($"[ShapeCardTarget] Card detected -> {shape}");
    }
}
