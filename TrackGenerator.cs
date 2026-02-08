using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TrackGenerator : MonoBehaviour
{
    [Header("Path")]
    
    public int seed = 0;
    public int pathPoints = 25;
    public float segmentLength = 25f;
    public float maxTurnAngle = 25f;
    public float maxSlopeAngle = 15f;

    [Header("Track")]
    public float trackWidth = 12f;
    public float sampleStep = 2f;

    [Header("Debug")]
    public bool drawGizmos = true;
    
    [Header("Material")]
    public Color trackColor = new Color(0.2f, 0.2f, 0.2f);
    public string shaderPath = ".poiyomi/Poiyomi Toon";
    public Texture2D normalMap;


    private List<Vector3> path;
    private Mesh mesh;

    void Start()
    {
        Generate();
    }

// TrackGenerator
    public System.Action OnTrackGenerated;

    public void Generate()
    {
        Random.InitState(seed);

        path = GeneratePath();
        var spline = new CatmullRomSpline(path);

        mesh = ExtrudeTrack(spline);
        GetComponent<MeshFilter>().mesh = mesh;

        AssignMaterial();

        // ----- ADD COLLIDER -----
        MeshCollider col = GetComponent<MeshCollider>();
        Physics.SyncTransforms(); // after adding the MeshCollider

        if (col == null)
            col = gameObject.AddComponent<MeshCollider>();

        col.sharedMesh = mesh;
        col.convex = false; // Static track, not moving
        col.inflateMesh = true;

        // Optional: mark as static for performance
        gameObject.isStatic = true;

        OnTrackGenerated?.Invoke(); // Notify listeners
    }




    // -------------------------
    // PATH GENERATION
    // -------------------------
    List<Vector3> GeneratePath()
    {
        List<Vector3> points = new List<Vector3>();

        Vector3 position = Vector3.zero;
        Vector3 direction = Vector3.forward;

        points.Add(position);

        for (int i = 0; i < pathPoints; i++)
        {
            float yaw = Random.Range(-maxTurnAngle, maxTurnAngle);
            float pitch = Random.Range(-maxSlopeAngle, maxSlopeAngle);

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            direction = rot * direction;

            position += direction.normalized * segmentLength;
            points.Add(position);
        }

        return points;
    }
    
    // -------------------------
    // ASIGN MATERIAL
    // -------------------------
    
    void AssignMaterial()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        Shader shader = Shader.Find(shaderPath);

        if (shader == null)
        {
            Debug.LogWarning(
                $"Shader '{shaderPath}' not found. Using Standard shader as fallback."
            );
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);

        // Base color
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", trackColor);

        // Normal map (Poiyomi + Standard compatible)
        if (normalMap != null)
        {
            if (mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normalMap);
                mat.EnableKeyword("_NORMALMAP");
            }
            else
            {
                Debug.LogWarning("Material does not support normal maps.");
            }
        }

        renderer.material = mat;
    }



    // -------------------------
    // MESH EXTRUSION
    // -------------------------
    Mesh ExtrudeTrack(CatmullRomSpline spline)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float length = spline.Length;
        int ringIndex = 0;

        for (float d = 0; d < length; d += sampleStep)
        {
            Vector3 pos = spline.GetPosition(d);
            Vector3 forward = spline.GetTangent(d).normalized;

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right);

            Vector3 leftPoint = pos - right * (trackWidth / 2f);
            Vector3 rightPoint = pos + right * (trackWidth / 2f);

            vertices.Add(leftPoint);
            vertices.Add(rightPoint);

            uvs.Add(new Vector2(0, d / length));
            uvs.Add(new Vector2(1, d / length));

            if (ringIndex > 0)
            {
                int baseIndex = ringIndex * 2;

                triangles.Add(baseIndex - 2);
                triangles.Add(baseIndex);
                triangles.Add(baseIndex - 1);

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex - 1);
            }

            ringIndex++;
        }

        Mesh m = new Mesh();
        m.SetVertices(vertices);
        m.SetTriangles(triangles, 0);
        m.SetUVs(0, uvs);
        m.RecalculateNormals();
        m.RecalculateBounds();

        return m;
    }

    // -------------------------
    // DEBUG
    // -------------------------
    void OnDrawGizmos()
    {
        if (!drawGizmos || path == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.Count - 1; i++)
            Gizmos.DrawLine(path[i], path[i + 1]);
    }
    
    //imnportant stuff to generate the car in the correct position
    public Vector3 GetStartPosition()
    {
        if (path == null || path.Count == 0)
            return Vector3.zero;

        return path[0];
    }

    public Vector3 GetStartForward()
    {
        if (path == null || path.Count < 2)
            return Vector3.forward;

        return (path[1] - path[0]).normalized;
    }

}


/// <summary>
/// ////
/// </summary>
public class CatmullRomSpline
{
    List<Vector3> points;
    float[] distances;
    public float Length { get; private set; }

    public CatmullRomSpline(List<Vector3> pts)
    {
        points = pts;
        PrecomputeDistances();
    }

    void PrecomputeDistances()
    {
        distances = new float[points.Count];
        Length = 0f;

        for (int i = 1; i < points.Count; i++)
        {
            Length += Vector3.Distance(points[i - 1], points[i]);
            distances[i] = Length;
        }
    }

    public Vector3 GetPosition(float distance)
    {
        float t = Mathf.Clamp01(distance / Length);
        float scaled = t * (points.Count - 1);
        int i = Mathf.FloorToInt(scaled);

        return GetPoint(i, scaled - i);
    }

    public Vector3 GetTangent(float distance)
    {
        float t = Mathf.Clamp01(distance / Length);
        float scaled = t * (points.Count - 1);
        int i = Mathf.FloorToInt(scaled);

        return GetDerivative(i, scaled - i);
    }

    Vector3 GetPoint(int i, float t)
    {
        Vector3 p0 = points[Mathf.Clamp(i - 1, 0, points.Count - 1)];
        Vector3 p1 = points[Mathf.Clamp(i, 0, points.Count - 1)];
        Vector3 p2 = points[Mathf.Clamp(i + 1, 0, points.Count - 1)];
        Vector3 p3 = points[Mathf.Clamp(i + 2, 0, points.Count - 1)];

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }

    Vector3 GetDerivative(int i, float t)
    {
        Vector3 p0 = points[Mathf.Clamp(i - 1, 0, points.Count - 1)];
        Vector3 p1 = points[Mathf.Clamp(i, 0, points.Count - 1)];
        Vector3 p2 = points[Mathf.Clamp(i + 1, 0, points.Count - 1)];
        Vector3 p3 = points[Mathf.Clamp(i + 2, 0, points.Count - 1)];

        return 0.5f * (
            (-p0 + p2) +
            2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t +
            3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t * t
        );
    }
}

