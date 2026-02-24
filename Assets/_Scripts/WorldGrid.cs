using UnityEngine;

public class WorldGrid : MonoBehaviour
{
    [Header("Grid")]
    public int width = 64;
    public int height = 64;
    public float cellSize = 2f;

    [Header("Resources")]
    [Range(0,1f)] public float treeChance = 0.08f;
    [Range(0,1f)] public float berryChance = 0.04f;
    public int treeWood = 50;
    public int berryFood = 30;
    public int seed = 12345;

    public enum ResourceType { None, Tree, Berry }

    public struct Tile
    {
        public ResourceType res;
        public int amount;
    }

    public Tile[] tiles;

    public int Index(int x, int z) => x + z * width;

    void Awake()
    {
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        Random.InitState(seed);
        tiles = new Tile[width * height];

        for (int z = 0; z < height; z++)
        for (int x = 0; x < width; x++)
        {
            var t = new Tile();

            float r = Random.value;
            if (r < treeChance)
            {
                t.res = ResourceType.Tree;
                t.amount = treeWood;
            }
            else if (r < treeChance + berryChance)
            {
                t.res = ResourceType.Berry;
                t.amount = berryFood;
            }
            else
            {
                t.res = ResourceType.None;
                t.amount = 0;
            }

            tiles[Index(x, z)] = t;
        }
    }

    public Vector3 CellToWorld(int x, int z)
{
    float halfW = width * cellSize * 0.5f;
    float halfH = height * cellSize * 0.5f;
    return transform.position + new Vector3((x + 0.5f) * cellSize - halfW, 0f, (z + 0.5f) * cellSize - halfH);
}

    public void WorldToCell(Vector3 world, out int x, out int z)
    {
        float halfW = width * cellSize * 0.5f;
        float halfH = height * cellSize * 0.5f;

        Vector3 local = world - transform.position;
        x = Mathf.FloorToInt((local.x + halfW) / cellSize);
        z = Mathf.FloorToInt((local.z + halfH) / cellSize);

        x = Mathf.Clamp(x, 0, width - 1);
        z = Mathf.Clamp(z, 0, height - 1);
    }

    public void ForceResourcePatch(int centerX, int centerZ, int radius, ResourceType type, int amountPerTile)
{
    for (int dz = -radius; dz <= radius; dz++)
    for (int dx = -radius; dx <= radius; dx++)
    {
        int x = centerX + dx;
        int z = centerZ + dz;
        if (x < 0 || z < 0 || x >= width || z >= height) continue;

        // circle
        if (dx*dx + dz*dz > radius*radius) continue;

        int idx = Index(x, z);
        var t = tiles[idx];
        t.res = type;
        t.amount = amountPerTile;
        tiles[idx] = t;
    }
}

    public bool IsBerry(int x, int z)
    {
        var t = tiles[Index(x, z)];
        return t.res == ResourceType.Berry && t.amount > 0;
    }

    public bool TryConsumeBerry(int x, int z, int amount)
    {
        int idx = Index(x, z);
        var t = tiles[idx];
        if (t.res != ResourceType.Berry || t.amount <= 0) return false;

        t.amount = Mathf.Max(0, t.amount - amount);
        tiles[idx] = t;
        return true;
    }

    void OnDrawGizmos()
    {
        if (tiles == null || tiles.Length != width * height) return;

        // draw a lightweight sample (skip some tiles so it doesn't lag in editor)
        int step = Mathf.Max(1, width / 64);

        for (int z = 0; z < height; z += step)
        for (int x = 0; x < width; x += step)
        {
            var t = tiles[Index(x, z)];
            var p = CellToWorld(x, z);

            // grid point
            Gizmos.color = new Color(1,1,1,0.05f);
            Gizmos.DrawWireCube(p, new Vector3(cellSize, 0.01f, cellSize));

            if (t.res == ResourceType.Tree)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
                Gizmos.DrawSphere(p + Vector3.up * 0.4f, 0.35f);
            }
            else if (t.res == ResourceType.Berry)
            {
                Gizmos.color = new Color(1f, 0.2f, 1f, 0.9f);
                Gizmos.DrawSphere(p + Vector3.up * 0.2f, 0.2f);
            }
        }
    }
}
