using UnityEngine;

public static class AgentStates
{
    public static void Tick(ref AgentData a, SimManager sim, float dt)
    {
        switch (a.state)
        {
            case AgentState.Idle: Idle(ref a, sim, dt); break;
            case AgentState.GoToBerry: GoToBerry(ref a, sim, dt); break;
            case AgentState.HarvestBerry: HarvestBerry(ref a, sim, dt); break;
            case AgentState.ReturnToBase: ReturnToBase(ref a, sim, dt); break;
        }
    }

    static void Idle(ref AgentData a, SimManager sim, float dt)
    {
        sim.WorldToCell(a.pos, out int ax, out int az);

        if (sim.TryFindNearestBerryCell(ax, az, sim.searchRadius, out int bx, out int bz))
        {
            a.berryX = bx; a.berryZ = bz;
            a.target = sim.grid.CellToWorld(bx, bz);
            a.state = AgentState.GoToBerry;
            return;
        }

        // fallback wander so they don't look dead
        if ((a.pos - a.target).sqrMagnitude < 0.5f)
        {
            int rx = Random.Range(0, sim.grid.width);
            int rz = Random.Range(0, sim.grid.height);
            a.target = sim.grid.CellToWorld(rx, rz);
        }
    }

    static void GoToBerry(ref AgentData a, SimManager sim, float dt)
    {
        if (!sim.grid.IsBerry(a.berryX, a.berryZ))
        {
            a.state = AgentState.Idle;
            return;
        }

        if ((a.pos - a.target).sqrMagnitude < 0.25f)
        {
            a.state = AgentState.HarvestBerry;
            a.harvestTimer = 0f;
        }
    }

    static void HarvestBerry(ref AgentData a, SimManager sim, float dt)
    {
        a.harvestTimer += dt;

        if (a.harvestTimer < sim.harvestInterval) return;
        a.harvestTimer = 0f;

        if (sim.grid.TryConsumeBerry(a.berryX, a.berryZ, 1))
        {
            a.carryFood++;

            if (a.carryFood >= sim.carryCapacity)
            {
                a.target = sim.factions[a.factionId].basePos;
                a.state = AgentState.ReturnToBase;
            }
        }
        else
        {
            a.state = AgentState.Idle;
        }
    }

    static void ReturnToBase(ref AgentData a, SimManager sim, float dt)
    {
        if ((a.pos - a.target).sqrMagnitude < 0.35f)
        {
            var f = sim.factions[a.factionId];
            f.food += a.carryFood;
            a.carryFood = 0;
            a.state = AgentState.Idle;
            sim.factions[a.factionId] = f;
        }
    }
}
