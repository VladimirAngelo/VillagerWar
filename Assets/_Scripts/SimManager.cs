using System.Collections.Generic;
using UnityEngine;

public class SimManager : MonoBehaviour
{
    [Header("Refs")]
    public WorldGrid grid;
    public AgentPuppet puppetPrefab;

    [Header("Population")]
    public int agentCount = 60;

    [Header("Factions")]
    public int factionCount = 4;
    public float factionRingRadius = 25f;
    public List<FactionData> factions = new();

    [Header("Movement")]
    public float minSpeed = 3.0f;
    public float maxSpeed = 4.5f;

    [Header("Avoidance")]
    public float avoidRadius = 1.2f;
    public float avoidStrength = 2.0f;
    public int avoidMaxNeighbors = 12;

    public List<AgentData> agents = new();

    void Start()
    {
        if (!grid) grid = FindFirstObjectByType<WorldGrid>();

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

            // base marker
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = $"Base_{i}";
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
                };

                agents.Add(a);

                var p = Instantiate(puppetPrefab, spawnPos, Quaternion.identity);
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

        // Update each agent
        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];

            // If reached target, pick a new one
            if ((a.pos - a.target).sqrMagnitude < 0.1f)
            {
                int rx = Random.Range(0, grid.width);
                int rz = Random.Range(0, grid.height);
                a.target = grid.CellToWorld(rx, rz);
            }

            Vector3 desired = (a.target - a.pos);
            desired.y = 0f;

            // Avoidance force
            Vector3 avoid = ComputeAvoidance(i);

            // Combine
            Vector3 dir = desired.normalized + avoid * avoidStrength;
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

            a.pos += dir * a.speed * dt;

            agents[i] = a; // write back
        }
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
}

public struct AgentData
{
    public int id;
    public int factionId;

    public Vector3 pos;
    public Vector3 target;
    public float speed;
}
