using System.Text;
using TMPro;
using UnityEngine;

public class SimHUD : MonoBehaviour
{
    public SimManager sim;
    public TMP_Text text;

    [Header("Refresh")]
    public float refreshRate = 0.2f;

    float t;
    readonly StringBuilder sb = new StringBuilder(2048);

    void Awake()
    {
        if (!sim) sim = FindFirstObjectByType<SimManager>();
        if (!text) text = GetComponentInChildren<TMP_Text>();
    }

    void Update()
    {
        if (!sim || !text) return;

        t -= Time.unscaledDeltaTime;
        if (t > 0f) return;
        t = refreshRate;

        sb.Clear();

        sb.AppendLine("<b>Living Factions</b>");
        sb.AppendLine($"TimeScale: <b>{Time.timeScale:0.##}x</b>");
        sb.AppendLine($"Agents: <b>{sim.agents.Count}</b>   Factions: <b>{sim.factions.Count}</b>");
        sb.AppendLine();

        for (int i = 0; i < sim.factions.Count; i++)
        {
            var f = sim.factions[i];
            string hex = ColorUtility.ToHtmlStringRGB(f.color);

            sb.Append($"<color=#{hex}><b>F{i}</b></color> ");
            sb.Append($"Pop: <b>{f.population}</b>  ");
            sb.Append($"Food: <b>{f.food}</b>  ");
            sb.Append($"Wood: <b>{f.wood}</b>");
            sb.AppendLine();
        }

        text.text = sb.ToString();
    }
}