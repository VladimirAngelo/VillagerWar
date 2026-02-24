using UnityEngine;

[System.Serializable]
public class FactionData
{
    public int id;
    public Color color = Color.white;

    // Base in world space for now
    public Vector3 basePos;

    public int food;
    public int wood;
    public int population;
}
