using System.Collections.Generic;
using UnityEngine;

public class SimManager : MonoBehaviour
{
    [Header("Refs")]
    public WorldGrid grid;
    public AgentPuppet puppetPrefab;

    [Header("Hierarchy")]
    public Transform puppetsRoot;
    public Transform basesRoot;

    [Header("Population")]
    public int agentCount = 60;

    [Header("Factions")]
    public int factionCount = 4;
    public float factionRingRadius = 25f;
    public List<FactionData> factions = new();

    [Header("Movement")]
    public float minSpeed = 3.0f;
    public float maxSpeed = 4.5f;

    [Header("Gathering")]
    public int carryCapacity = 5;
    public float harvestInterval = 0.6f;
    public int searchRadius = 25;

    [Header("Avoidance")]
    public float avoidRadius = 1.2f;
    public float avoidStrength = 2.0f;
    public int avoidMaxNeighbors = 12;

    public List<AgentData> agents = new();

    void Start()
    {
        if (!grid) grid = FindFirstObjectByType<WorldGrid>();
        if (!puppetsRoot) puppetsRoot = new GameObject("_Puppets").transform;
        if (!basesRoot) basesRoot = new GameObject("_Bases").transform;

        BuildFactions();
        SpawnAgents();

        Debug.Log($"Factions: {factions.Count} | Agents: {agents.Count}");
    }

    void BuildFactions()
    {
        factions.Clear();

        Vector3 center = grid.CellToWorld(grid.width / 2, grid.height / 2);

        for (int i = 0; i < factionCount; i++)
        {
            float a = (i / (float)factionCount) * Mathf.PI * 2f;
            Vector3 basePos = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * factionRingRadius;

            var f = new FactionData
            {
                id = i,
                color = Color.HSVToRGB(i / (float)factionCount, 0.8f, 0.9f),
                basePos = basePos,
                food = 0,
                wood = 0,
                population = 0
            };

            factions.Add(f);

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Base_{i:00}";
            marker.transform.SetParent(basesRoot, worldPositionStays: true);
            marker.transform.position = basePos + Vector3.up * 0.5f;
            marker.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            marker.GetComponent<Renderer>().material.color = f.color;
            Destroy(marker.GetComponent<Collider>());
        }
    }

    void SpawnAgents()
    {
        agents.Clear();

        // even-ish split
        int perFaction = Mathf.Max(1, agentCount / Mathf.Max(1, factionCount));
        int spawned = 0;

        for (int fi = 0; fi < factions.Count; fi++)
        {
            var f = factions[fi];

            for (int n = 0; n < perFaction && spawned < agentCount; n++)
            {
                Vector3 spawnPos = f.basePos + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));

                var a = new AgentData
                {
                    id = spawned,
                    factionId = f.id,
                    pos = spawnPos,
                    target = spawnPos,
                    speed = Random.Range(minSpeed, maxSpeed),
                    state = AgentState.Idle,
                    carryFood = 0,
                    harvestTimer = 0f,
                    berryX = -1,
                    berryZ = -1,
                };

                agents.Add(a);

                var p = Instantiate(puppetPrefab, spawnPos, Quaternion.identity, puppetsRoot);
                p.name = $"A_{a.id:000}_F{a.factionId}";
                p.Init(a.id, f.color);

                f.population++;
                spawned++;
            }

            factions[fi] = f; // write back updates to population
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            AgentStates.Tick(ref a, this, dt);
            MoveAgent(ref a, dt);

            agents[i] = a;
        }
    }

    void MoveAgent(ref AgentData a, float dt)
    {
        Vector3 desired = a.target - a.pos;
        desired.y = 0f;

        Vector3 avoid = ComputeAvoidance(a.id);
        Vector3 dir = desired.sqrMagnitude > 0.0001f ? desired.normalized : Vector3.zero;
        dir = dir + avoid * avoidStrength;

        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
        a.pos += dir * a.speed * dt;
    }

    Vector3 ComputeAvoidance(int agentIndex)
    {
        var me = agents[agentIndex];
        Vector3 push = Vector3.zero;

        float r2 = avoidRadius * avoidRadius;
        int count = 0;

        // O(N^2) for now. Fine at 60–300 agents.
        // Later we’ll add a spatial hash grid.
        for (int j = 0; j < agents.Count; j++)
        {
            if (j == agentIndex) continue;

            Vector3 d = me.pos - agents[j].pos;
            d.y = 0f;

            float dist2 = d.sqrMagnitude;
            if (dist2 > r2 || dist2 < 0.000001f) continue;

            // stronger push when closer
            float t = 1f - Mathf.Sqrt(dist2) / avoidRadius;
            push += d.normalized * t;

            count++;
            if (count >= avoidMaxNeighbors) break;
        }

        if (count == 0) return Vector3.zero;
        return push / count;
    }

    public bool WorldToCell(Vector3 world, out int cx, out int cz)
    {
        grid.WorldToCell(world, out cx, out cz);
        return true;
    }

    public bool TryFindNearestBerryCell(int fromX, int fromZ, int radius, out int outX, out int outZ)
    {
        outX = fromX; outZ = fromZ;
        int bestDist = int.MaxValue;
        bool found = false;

        for (int r = 1; r <= radius; r++)
        {
            for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                int x = fromX + dx;
                int z = fromZ + dz;
                if (x < 0 || z < 0 || x >= grid.width || z >= grid.height) continue;

                if (!grid.IsBerry(x, z)) continue;

                int dist = Mathf.Abs(dx) + Mathf.Abs(dz);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    outX = x; outZ = z;
                    found = true;
                }
            }
            if (found) break;
        }

        return found;
    }
}

public enum AgentState { Idle, GoToBerry, HarvestBerry, ReturnToBase }

public struct AgentData
{
    public int id;
    public int factionId;

    public Vector3 pos;
    public Vector3 target;
    public float speed;

    public AgentState state;

    public int carryFood;
    public float harvestTimer;

    // berry target in grid coords (so we don’t lose it)
    public int berryX, berryZ;
}
