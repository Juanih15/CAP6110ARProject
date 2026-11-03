using UnityEngine;

// Pure math + text helpers. No MonoBehaviour, so it's easy to test and reuse
// from both the viewer and the quiz.

public enum ShapeKind { Square, Circle, Rectangle }
public enum ViewMode { TwoD, ThreeD }
public enum Quantity { Perimeter, Area, SurfaceArea, Volume }

[System.Serializable]
public class ShapeDims
{
    // All values in centimeters.
    // Square/Cube:          a = side
    // Circle/Sphere:        a = radius
    // Rectangle/Prism:      a = width, b = length, c = height (3D only)
    public float a = 4f;
    public float b = 6f;
    public float c = 3f;

    public ShapeDims Copy() => new ShapeDims { a = a, b = b, c = c };
}

public static class Geometry
{
    public static string Name(ShapeKind k, ViewMode m)
    {
        switch (k)
        {
            case ShapeKind.Square:    return m == ViewMode.TwoD ? "Square" : "Cube";
            case ShapeKind.Circle:    return m == ViewMode.TwoD ? "Circle" : "Sphere";
            case ShapeKind.Rectangle: return m == ViewMode.TwoD ? "Rectangle" : "Rectangular Prism";
        }
        return "?";
    }

    // 2D mode shows perimeter + area, 3D mode shows surface area + volume.
    public static Quantity[] QuantitiesFor(ViewMode m) =>
        m == ViewMode.TwoD
            ? new[] { Quantity.Perimeter, Quantity.Area }
            : new[] { Quantity.SurfaceArea, Quantity.Volume };

    public static float Compute(ShapeKind k, ViewMode m, ShapeDims d, Quantity q)
    {
        float a = d.a, b = d.b, c = d.c, pi = Mathf.PI;
        switch (k)
        {
            case ShapeKind.Square:
                if (q == Quantity.Perimeter)   return 4 * a;
                if (q == Quantity.Area)        return a * a;
                if (q == Quantity.SurfaceArea) return 6 * a * a;
                return a * a * a;
            case ShapeKind.Circle:
                if (q == Quantity.Perimeter)   return 2 * pi * a;          // circumference
                if (q == Quantity.Area)        return pi * a * a;
                if (q == Quantity.SurfaceArea) return 4 * pi * a * a;
                return 4f / 3f * pi * a * a * a;
            case ShapeKind.Rectangle:
                if (q == Quantity.Perimeter)   return 2 * (a + b);
                if (q == Quantity.Area)        return a * b;
                if (q == Quantity.SurfaceArea) return 2 * (a * b + b * c + a * c);
                return a * b * c;
        }
        return 0;
    }

    public static string Formula(ShapeKind k, Quantity q)
    {
        switch (k)
        {
            case ShapeKind.Square:
                return q == Quantity.Perimeter ? "P = 4s" : q == Quantity.Area ? "A = s²"
                     : q == Quantity.SurfaceArea ? "SA = 6s²" : "V = s³";
            case ShapeKind.Circle:
                return q == Quantity.Perimeter ? "C = 2πr" : q == Quantity.Area ? "A = πr²"
                     : q == Quantity.SurfaceArea ? "SA = 4πr²" : "V = (4/3)πr³";
            case ShapeKind.Rectangle:
                return q == Quantity.Perimeter ? "P = 2(w + l)" : q == Quantity.Area ? "A = w·l"
                     : q == Quantity.SurfaceArea ? "SA = 2(wl + lh + wh)" : "V = w·l·h";
        }
        return "";
    }

    public static string Label(Quantity q, ShapeKind k) =>
        q == Quantity.Perimeter ? (k == ShapeKind.Circle ? "Circumference" : "Perimeter")
      : q == Quantity.Area ? "Area"
      : q == Quantity.SurfaceArea ? "Surface area"
      : "Volume";

    public static string Unit(Quantity q) =>
        q == Quantity.Perimeter ? "cm" : q == Quantity.Volume ? "cm³" : "cm²";

    public static string Num(float v) => v.ToString("0.##");

    // "Side: 4 cm" style dimension lines.
    public static string DimensionText(ShapeKind k, ViewMode m, ShapeDims d)
    {
        switch (k)
        {
            case ShapeKind.Square: return $"Side: {Num(d.a)} cm";
            case ShapeKind.Circle: return $"Radius: {Num(d.a)} cm";
            default:
                string s = $"Width: {Num(d.a)} cm\nLength: {Num(d.b)} cm";
                return m == ViewMode.ThreeD ? s + $"\nHeight: {Num(d.c)} cm" : s;
        }
    }

    // Full info panel text for the Shape Viewer.
    public static string Summary(ShapeKind k, ViewMode m, ShapeDims d)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>{Name(k, m)}</b>");
        sb.AppendLine(DimensionText(k, m, d));
        foreach (var q in QuantitiesFor(m))
            sb.AppendLine($"{Label(q, k)}: {Num(Compute(k, m, d, q))} {Unit(q)}   <size=80%>({Formula(k, q)})</size>");
        return sb.ToString();
    }

    // Natural-language description for quiz prompts.
    public static string Describe(ShapeKind k, ViewMode m, ShapeDims d)
    {
        switch (k)
        {
            case ShapeKind.Square:
                return $"A {Name(k, m).ToLower()} has sides of {Num(d.a)} cm.";
            case ShapeKind.Circle:
                return $"A {Name(k, m).ToLower()} has a radius of {Num(d.a)} cm.";
            default:
                return m == ViewMode.TwoD
                    ? $"A rectangle is {Num(d.a)} cm wide and {Num(d.b)} cm long."
                    : $"A rectangular prism is {Num(d.a)} cm × {Num(d.b)} cm × {Num(d.c)} cm.";
        }
    }
}
