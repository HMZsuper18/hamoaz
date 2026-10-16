using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LowPolySea : MonoBehaviour
{
    [Header("Grid")]
    public int resolution = 50;   // quads per side (more = more waves)
    public float size = 60;       // world size in meters

    [Header("Waves")]
    public float waveHeight = 0.6f;
    public float waveFrequency = 0.5f;
    public float waveSpeed = 1.2f;
    [Range(0.5f, 3f)]
    public float sharpness = 1.8f; // higher = pointier, choppier peaks

    Mesh mesh;
    Vector3[] basePositions;

    void Awake() => BuildMesh();

    void Start()
    {
        if (mesh == null) BuildMesh();
    }

    void BuildMesh()
    {
        Vector3[] verts = new Vector3[resolution * resolution * 4];
        int[] tris = new int[resolution * resolution * 6];
        int v = 0, t = 0;
        float cell = size / resolution;
        float half = size * 0.5f;

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float x0 = -half + x * cell, x1 = x0 + cell;
                float z0 = -half + z * cell, z1 = z0 + cell;
                // 4 separate verts per quad => sharp flat faces
                verts[v + 0] = new Vector3(x0, 0, z0);
                verts[v + 1] = new Vector3(x0, 0, z1);
                verts[v + 2] = new Vector3(x1, 0, z1);
                verts[v + 3] = new Vector3(x1, 0, z0);
                tris[t++] = v; tris[t++] = v + 1; tris[t++] = v + 2;
                tris[t++] = v; tris[t++] = v + 2; tris[t++] = v + 3;
                v += 4;
            }
        }

        mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts;
        mesh.triangles = tris;
        GetComponent<MeshFilter>().mesh = mesh;
        basePositions = (Vector3[])verts.Clone();
    }

    void Update()
    {
        if (mesh == null || basePositions == null) BuildMesh();
        Vector3[] verts = mesh.vertices;
        float t = Time.time * waveSpeed;
        for (int i = 0; i < verts.Length; i++)
        {
            float x = basePositions[i].x, z = basePositions[i].z;
            float s = Mathf.Sin((x + t) * waveFrequency)
                    * Mathf.Cos((z + t * 0.7f) * waveFrequency);
            s = Mathf.Pow(Mathf.Abs(s), sharpness) * Mathf.Sign(s);
            verts[i].y = s * waveHeight;
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals(); // each face gets its own normal = sharp
        mesh.RecalculateBounds();
    }
}