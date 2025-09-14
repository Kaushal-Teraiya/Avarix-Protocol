using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SegmentedLine : MonoBehaviour
{
    public Transform startPoint;
    public Transform middlePoint;
    public Transform endPoint;
    public bool useCurvyLine = false; // Toggle this in Inspector

    private LineRenderer lineRenderer;

    [Range(5, 50)]
    public int smoothness = 20; // More points = smoother curve

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (useCurvyLine)
        {
            DrawCurvyLine();
        }
        else
        {
            DrawSegmentedLine();
        }
    }

    void DrawSegmentedLine()
    {
        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, startPoint.position);
        lineRenderer.SetPosition(1, middlePoint.position);
        lineRenderer.SetPosition(2, endPoint.position);
    }

    void DrawCurvyLine()
    {
        lineRenderer.positionCount = smoothness;
        for (int i = 0; i < smoothness; i++)
        {
            float t = i / (smoothness - 1f);
            Vector3 point = GetQuadraticBezierPoint(
                t,
                startPoint.position,
                middlePoint.position,
                endPoint.position
            );
            lineRenderer.SetPosition(i, point);
        }
    }

    // Quadratic Bezier: P = (1-t)^2 * A + 2(1-t)t * B + t^2 * C
    Vector3 GetQuadraticBezierPoint(float t, Vector3 a, Vector3 b, Vector3 c)
    {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * a + 2f * oneMinusT * t * b + t * t * c;
    }
}
