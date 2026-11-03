using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Study / Quiz" screen. Questions are generated in-app from Geometry.cs,
// so there's no database. Three question types:
//   1. Calculate   "A cube has sides of 4 cm. What is its volume?"
//   2. Formula     "Which formula gives the surface area of a sphere?"
//   3. Identify    "Which 3D solid pairs with a circle in this app?"
public class QuizManager : MonoBehaviour
{
    public int questionsPerQuiz = 5;

    [Header("UI")]
    public TMP_Text progressText;      // "Question 2 of 5"
    public TMP_Text promptText;
    public Button[] optionButtons;     // 3 buttons
    public TMP_Text[] optionLabels;
    public Button checkButton;
    public TMP_Text feedbackText;
    public TMP_Text scoreText;         // "Correct answers: 1 / 2"
    public ShapeViewerUI viewerUI;     // for "Return to Shape Viewer"

    private class Question { public string prompt; public string[] options; public int correct; public string explain; }

    private readonly List<Question> questions = new List<Question>();
    private int index, selected = -1, correctCount, answered;
    private bool checkedThis;

    void OnEnable() { StartQuiz(); }

    public void StartQuiz()
    {
        questions.Clear();
        for (int i = 0; i < questionsPerQuiz; i++) questions.Add(MakeQuestion());
        index = correctCount = answered = 0;
        Show();
    }

    // ---------- generation ----------

    Question MakeQuestion()
    {
        var k = (ShapeKind)Random.Range(0, 3);
        var m = Random.value < 0.5f ? ViewMode.TwoD : ViewMode.ThreeD;
        var quantities = Geometry.QuantitiesFor(m);
        var q = quantities[Random.Range(0, quantities.Length)];
        int type = Random.Range(0, 10);   // 60% calculate, 30% formula, 10% identify

        if (type < 6) return Calculate(k, m, q);
        if (type < 9) return FormulaQ(k, m, q);
        return Identify(k);
    }

    Question Calculate(ShapeKind k, ViewMode m, Quantity q)
    {
        var d = new ShapeDims { a = Random.Range(2, 10), b = Random.Range(2, 10), c = Random.Range(2, 10) };
        float right = Geometry.Compute(k, m, d, q);
        string unit = Geometry.Unit(q);

        // Distractors = the *other* quantities for the same shape, plus a common slip.
        // Mirrors the sketch: cube s=4 -> 64 (volume), 96 (surface area), 16 (face area).
        var wrong = new List<float>();
        foreach (Quantity other in System.Enum.GetValues(typeof(Quantity)))
        {
            if (other == q) continue;
            float v = Geometry.Compute(k, m, d, other);
            if (!Mathf.Approximately(v, right) && !wrong.Exists(w => Mathf.Approximately(w, v))) wrong.Add(v);
        }
        while (wrong.Count < 2) wrong.Add(right * Random.Range(2, 4));

        return Build(
            $"{Geometry.Describe(k, m, d)}\nWhat is its {Geometry.Label(q, k).ToLower()}?",
            Geometry.Num(right) + " " + unit,
            new[] { Geometry.Num(wrong[0]) + " " + unit, Geometry.Num(wrong[1]) + " " + unit },
            $"{Geometry.Formula(k, q)} = {Geometry.Num(right)} {unit}");
    }

    Question FormulaQ(ShapeKind k, ViewMode m, Quantity q)
    {
        var others = new List<string>();
        foreach (Quantity o in System.Enum.GetValues(typeof(Quantity)))
            if (o != q) others.Add(Geometry.Formula(k, o));
        Shuffle(others);
        return Build(
            $"Which formula gives the {Geometry.Label(q, k).ToLower()} of a {Geometry.Name(k, m).ToLower()}?",
            Geometry.Formula(k, q), new[] { others[0], others[1] }, "");
    }

    Question Identify(ShapeKind k)
    {
        var wrong = new List<string>();
        foreach (ShapeKind o in System.Enum.GetValues(typeof(ShapeKind)))
            if (o != k) wrong.Add(Geometry.Name(o, ViewMode.ThreeD));
        return Build(
            $"In this app, which 3D solid is paired with a {Geometry.Name(k, ViewMode.TwoD).ToLower()}?",
            Geometry.Name(k, ViewMode.ThreeD), wrong.ToArray(), "");
    }

    Question Build(string prompt, string right, string[] wrong, string explain)
    {
        var opts = new List<string> { right, wrong[0], wrong[1] };
        Shuffle(opts);
        return new Question { prompt = prompt, options = opts.ToArray(), correct = opts.IndexOf(right), explain = explain };
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        { int j = Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
    }

    // ---------- UI flow ----------

    void Show()
    {
        if (index >= questions.Count)
        {
            progressText.text = "Quiz complete";
            promptText.text = $"You got {correctCount} of {questions.Count} correct.";
            foreach (var b in optionButtons) b.gameObject.SetActive(false);
            checkButton.GetComponentInChildren<TMP_Text>().text = "Try Again";
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(StartQuiz);
            feedbackText.text = "";
            return;
        }

        var q = questions[index];
        progressText.text = $"Question {index + 1} of {questions.Count}";
        promptText.text = q.prompt;
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int captured = i;
            optionButtons[i].gameObject.SetActive(true);
            optionButtons[i].interactable = true;
            optionLabels[i].text = $"{(char)('A' + i)}. {q.options[i]}";
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => Select(captured));
            SetColor(optionButtons[i], Color.white);
        }
        selected = -1; checkedThis = false;
        feedbackText.text = "";
        checkButton.GetComponentInChildren<TMP_Text>().text = "Check Answer";
        checkButton.onClick.RemoveAllListeners();
        checkButton.onClick.AddListener(Check);
        UpdateScore();
    }

    void Select(int i)
    {
        if (checkedThis) return;
        selected = i;
        for (int j = 0; j < optionButtons.Length; j++)
            SetColor(optionButtons[j], j == i ? new Color(0.8f, 0.9f, 1f) : Color.white);
    }

    void Check()
    {
        if (checkedThis) { index++; Show(); return; }   // second press = Next
        if (selected < 0) { feedbackText.text = "Pick an answer first."; return; }

        var q = questions[index];
        bool ok = selected == q.correct;
        answered++; if (ok) correctCount++;
        checkedThis = true;

        SetColor(optionButtons[q.correct], new Color(0.7f, 0.95f, 0.7f));
        if (!ok) SetColor(optionButtons[selected], new Color(1f, 0.75f, 0.75f));
        feedbackText.text = (ok ? "Correct!" : "Not quite.") + (q.explain != "" ? "  " + q.explain : "");
        checkButton.GetComponentInChildren<TMP_Text>().text = index + 1 < questions.Count ? "Next" : "Finish";
        UpdateScore();
    }

    void UpdateScore() { scoreText.text = $"Correct answers: {correctCount} / {answered}"; }

    static void SetColor(Button b, Color c) { var img = b.GetComponent<Image>(); if (img) img.color = c; }

    public void ReturnToViewer() { viewerUI.ShowViewer(); }
}
