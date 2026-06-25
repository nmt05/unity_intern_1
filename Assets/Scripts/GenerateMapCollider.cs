using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class GreedyColliderGenerator : MonoBehaviour
{
    [Header("Block Settings")]
    public float blockSize = 2f;

    [Header("Collect Settings")]
    [Tooltip("Nếu bật, chỉ lấy object con trực tiếp của Map. Nên bật nếu mỗi block là con trực tiếp của Map.")]
    public bool onlyDirectChildren = true;

    private readonly HashSet<Vector3Int> blocks = new();

    private readonly List<Vector3> vertices = new();
    private readonly List<int> triangles = new();

    [ContextMenu("Generate Greedy Collider")]
    public void GenerateCollider()
    {
        blocks.Clear();
        vertices.Clear();
        triangles.Clear();

        CollectBlocks();

        if (blocks.Count == 0)
        {
            Debug.LogWarning("No blocks found.");
            return;
        }

        GenerateTopFaces();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshCollider mc = GetComponent<MeshCollider>();
        mc.sharedMesh = null;
        mc.sharedMesh = mesh;

        Debug.Log($"Greedy Collider Generated | Blocks={blocks.Count} | Tris={triangles.Count / 3}");
    }

    void CollectBlocks()
    {
        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            Transform t = mf.transform;

            // Bỏ qua item
            if (t.CompareTag("Item"))
                continue;

            Vector3 localPos = transform.InverseTransformPoint(t.position);

            int x = Mathf.RoundToInt(localPos.x / blockSize);
            int y = Mathf.RoundToInt(localPos.y / blockSize);
            int z = Mathf.RoundToInt(localPos.z / blockSize);

            blocks.Add(new Vector3Int(x, y, z));
        }
    }
    void AddBlockFromTransform(Transform blockTransform)
    {
        // Không dùng Renderer.bounds vì cỏ, táo hoặc chi tiết nhô lên sẽ làm bounds sai.
        // Chỉ lấy vị trí transform của block theo lưới 2x2.
        Vector3 localPos = transform.InverseTransformPoint(blockTransform.position);

        int x = Mathf.RoundToInt(localPos.x / blockSize);
        int y = Mathf.RoundToInt(localPos.y / blockSize);
        int z = Mathf.RoundToInt(localPos.z / blockSize);

        blocks.Add(new Vector3Int(x, y, z));
    }

    void GenerateTopFaces()
    {
        int minX = int.MaxValue;
        int maxX = int.MinValue;

        int minZ = int.MaxValue;
        int maxZ = int.MinValue;

        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (Vector3Int b in blocks)
        {
            minX = Mathf.Min(minX, b.x);
            maxX = Mathf.Max(maxX, b.x);

            minZ = Mathf.Min(minZ, b.z);
            maxZ = Mathf.Max(maxZ, b.z);

            minY = Mathf.Min(minY, b.y);
            maxY = Mathf.Max(maxY, b.y);
        }

        int width = maxX - minX + 1;
        int height = maxZ - minZ + 1;

        for (int y = minY; y <= maxY; y++)
        {
            bool[,] mask = new bool[width, height];

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3Int p = new Vector3Int(x, y, z);

                    bool exists = blocks.Contains(p);
                    bool topVisible = exists && !blocks.Contains(p + Vector3Int.up);

                    mask[x - minX, z - minZ] = topVisible;
                }
            }

            GreedyMask(mask, minX, minZ, y);
        }
    }

    void GreedyMask(bool[,] mask, int offsetX, int offsetZ, int y)
    {
        int sizeX = mask.GetLength(0);
        int sizeZ = mask.GetLength(1);

        for (int z = 0; z < sizeZ; z++)
        {
            for (int x = 0; x < sizeX; x++)
            {
                if (!mask[x, z])
                    continue;

                int quadWidth = 1;

                while (x + quadWidth < sizeX && mask[x + quadWidth, z])
                {
                    quadWidth++;
                }

                int quadHeight = 1;
                bool canExpand = true;

                while (z + quadHeight < sizeZ && canExpand)
                {
                    for (int k = 0; k < quadWidth; k++)
                    {
                        if (!mask[x + k, z + quadHeight])
                        {
                            canExpand = false;
                            break;
                        }
                    }

                    if (canExpand)
                        quadHeight++;
                }

                AddTopQuad(
                    x + offsetX,
                    z + offsetZ,
                    y,
                    quadWidth,
                    quadHeight);

                for (int dz = 0; dz < quadHeight; dz++)
                {
                    for (int dx = 0; dx < quadWidth; dx++)
                    {
                        mask[x + dx, z + dz] = false;
                    }
                }
            }
        }
    }

    void AddTopQuad(int x, int z, int y, int width, int height)
    {
        float s = blockSize;
        float half = s * 0.5f;

        // Vì pivot block nằm ở tâm, block tại grid (0,0,0) chiếm:
        // X: -1 -> 1
        // Y: -1 -> 1
        // Z: -1 -> 1
        // Nên phải trừ half để collider không lệch +1 ở X/Z/Y.
        float topY = (y + 0.5f) * s + 1f;

        Vector3 v0 = new Vector3(
            x * s - half,
            topY,
            z * s - half);

        Vector3 v1 = new Vector3(
            (x + width) * s - half,
            topY,
            z * s - half);

        Vector3 v2 = new Vector3(
            (x + width) * s - half,
            topY,
            (z + height) * s - half);

        Vector3 v3 = new Vector3(
            x * s - half,
            topY,
            (z + height) * s - half);

        int start = vertices.Count;

        vertices.Add(v0);
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        triangles.Add(start + 0);
        triangles.Add(start + 2);
        triangles.Add(start + 1);

        triangles.Add(start + 0);
        triangles.Add(start + 3);
        triangles.Add(start + 2);
    }
}
