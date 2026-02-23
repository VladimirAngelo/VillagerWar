using UnityEngine;

public class AgentPuppet : MonoBehaviour
{
    public int agentId;
    Renderer rend;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
    }

    public void Init(int id, Color c)
    {
        agentId = id;
        if (rend) rend.material.color = c;
        transform.localScale = new Vector3(0.7f, 1.2f, 0.7f);
    }

    void Update()
    {
        var sim = FindFirstObjectByType<SimManager>();
        if (!sim) return;
        if (agentId < 0 || agentId >= sim.agents.Count) return;

        Vector3 target = sim.agents[agentId].pos;
        transform.position = Vector3.Lerp(transform.position, target, 15f * Time.deltaTime);
    }
}