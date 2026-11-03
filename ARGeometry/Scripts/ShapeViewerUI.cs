using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The "Shape Viewer" screen from the proposal sketch. Wire the fields in the
// Inspector, then hook buttons to the public methods (Button > OnClick).
public class ShapeViewerUI : MonoBehaviour
{
    [Header("Info")]
    public TMP_Text infoText;          // name, dimensions, results, formulas

    [Header("Mode toggle buttons")]
    public Button twoDButton;
    public Button threeDButton;
    public TMP_Text twoDLabel;         // "2D Square"
    public TMP_Text threeDLabel;       // "3D Cube"

    [Header("Dimension sliders (cm)")]
    public Slider sliderA;             // side / radius / width
    public Slider sliderB;             // length (rectangle only)
    public Slider sliderC;             // height (prism only)
    public TMP_Text labelA, labelB, labelC;

    [Header("Panels")]
    public GameObject shapeMenuPanel;  // "Choose / Insert Shape" popup
    public GameObject viewerPanel;
    public GameObject quizPanel;

    private ShapeController sc;
    private bool syncing;

    void Start()
    {
        sc = ShapeController.Instance;
        sc.Changed += Refresh;

        twoDButton.onClick.AddListener(() => sc.SetMode(ViewMode.TwoD));
        threeDButton.onClick.AddListener(() => sc.SetMode(ViewMode.ThreeD));
        sliderA.onValueChanged.AddListener(v => { if (!syncing) sc.SetDimension(0, v); });
        sliderB.onValueChanged.AddListener(v => { if (!syncing) sc.SetDimension(1, v); });
        sliderC.onValueChanged.AddListener(v => { if (!syncing) sc.SetDimension(2, v); });

        if (shapeMenuPanel) shapeMenuPanel.SetActive(false);
        ShowViewer();
        Refresh();
    }

    void OnDestroy() { if (sc != null) sc.Changed -= Refresh; }

    void Refresh()
    {
        var k = sc.kind; var d = sc.dims;
        infoText.text = Geometry.Summary(k, sc.mode, d);

        twoDLabel.text   = "2D " + Geometry.Name(k, ViewMode.TwoD);
        threeDLabel.text = "3D " + Geometry.Name(k, ViewMode.ThreeD);
        twoDButton.interactable   = sc.mode != ViewMode.TwoD;
        threeDButton.interactable = sc.mode != ViewMode.ThreeD;

        bool rect = k == ShapeKind.Rectangle;
        sliderB.gameObject.SetActive(rect);
        sliderC.gameObject.SetActive(rect && sc.mode == ViewMode.ThreeD);
        labelA.text = k == ShapeKind.Square ? "Side" : k == ShapeKind.Circle ? "Radius" : "Width";
        if (labelB) labelB.text = "Length";
        if (labelC) labelC.text = "Height";

        // Push values back into sliders without re-triggering SetDimension.
        syncing = true;
        sliderA.value = d.a; sliderB.value = d.b; sliderC.value = d.c;
        syncing = false;
    }

    // ---------- button hooks ----------

    public void OpenShapeMenu()  { shapeMenuPanel.SetActive(true); }
    public void PickSquare()     { Pick(ShapeKind.Square); }
    public void PickCircle()     { Pick(ShapeKind.Circle); }
    public void PickRectangle()  { Pick(ShapeKind.Rectangle); }

    void Pick(ShapeKind k)
    {
        // Manual fallback: shape shows in front of the camera, no card needed.
        sc.AttachTo(manualAnchor != null ? manualAnchor : null);
        sc.SetShape(k);
        shapeMenuPanel.SetActive(false);
    }

    [Tooltip("Where manually chosen shapes appear, e.g. an empty child of the AR Camera ~0.3 m forward, " +
             "or a Vuforia Ground Plane anchor later.")]
    public Transform manualAnchor;

    public void ShowViewer() { viewerPanel.SetActive(true);  quizPanel.SetActive(false); }
    public void ShowQuiz()   { viewerPanel.SetActive(false); quizPanel.SetActive(true); }
}
