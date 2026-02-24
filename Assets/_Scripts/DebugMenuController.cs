using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugMenuController : MonoBehaviour
{
    [Header("Menu")]
    public GameObject menuRoot;          // the DebugMenu panel
    public Button resumeButton;

    [Header("HUD")]
    public GameObject hudRoot;           // your SimHUD panel root (or any HUD parent)
    public Toggle showHudToggle;

    [Header("Time")]
    public Toggle pauseToggle;
    public Slider speedSlider;
    public TMP_Text speedLabel;

    [Header("Debug")]
    public bool logStateChanges = true;

    bool isOpen;
    float lastNonZeroScale = 1f;

    void Awake()
    {
        if (!menuRoot) menuRoot = gameObject;

        // Wire UI events safely
        if (resumeButton) resumeButton.onClick.AddListener(CloseMenu);

        if (showHudToggle) showHudToggle.onValueChanged.AddListener(SetHudVisible);
        if (pauseToggle) pauseToggle.onValueChanged.AddListener(SetPaused);
        if (speedSlider) speedSlider.onValueChanged.AddListener(SetSpeedFromSlider);

        // Initialize UI from current state
        if (menuRoot) menuRoot.SetActive(false);

        float ts = Mathf.Max(0f, Time.timeScale);
        if (ts > 0f) lastNonZeroScale = ts;

        if (speedSlider) speedSlider.value = ts;
        UpdateSpeedLabel(ts);

        if (pauseToggle) pauseToggle.isOn = (Time.timeScale == 0f);
        if (showHudToggle && hudRoot) showHudToggle.isOn = hudRoot.activeSelf;

        ApplyCursorState();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (isOpen) CloseMenu();
            else OpenMenu();
        }
    }

    public void OpenMenu()
    {
        isOpen = true;
        if (menuRoot) menuRoot.SetActive(true);

        float ts = Mathf.Max(0f, Time.timeScale);
        if (ts > 0f) lastNonZeroScale = ts;

        ApplyCursorState();

        // keep UI consistent without triggering callbacks/state changes
        if (pauseToggle) pauseToggle.SetIsOnWithoutNotify(Mathf.Approximately(ts, 0f));
        if (speedSlider) speedSlider.SetValueWithoutNotify(ts);
        UpdateSpeedLabel(ts);
        LogState("OpenMenu");
    }

    public void CloseMenu()
    {
        isOpen = false;
        if (menuRoot) menuRoot.SetActive(false);

        ApplyCursorState();

        // keep slider/label updated without changing simulation state
        float ts = Mathf.Max(0f, Time.timeScale);
        if (pauseToggle) pauseToggle.SetIsOnWithoutNotify(Mathf.Approximately(ts, 0f));
        if (speedSlider) speedSlider.SetValueWithoutNotify(ts);
        UpdateSpeedLabel(ts);
        LogState("CloseMenu");
    }

    void SetHudVisible(bool on)
    {
        if (hudRoot) hudRoot.SetActive(on);
    }

    void SetPaused(bool paused)
    {
        LogState($"SetPaused(start, paused={paused})");
        if (paused)
        {
            if (Time.timeScale > 0f) lastNonZeroScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = Mathf.Max(0.1f, lastNonZeroScale);
        }

        if (speedSlider) speedSlider.SetValueWithoutNotify(Time.timeScale);
        UpdateSpeedLabel(Time.timeScale);
        LogState($"SetPaused(end, paused={paused})");
    }

    void SetSpeedFromSlider(float value)
    {
        float s = Mathf.Max(0f, value);

        if (s <= 0.0001f)
        {
            if (Time.timeScale > 0f) lastNonZeroScale = Time.timeScale;
            Time.timeScale = 0f;
            if (pauseToggle) pauseToggle.SetIsOnWithoutNotify(true);
        }
        else
        {
            lastNonZeroScale = s;
            Time.timeScale = s;
            if (pauseToggle) pauseToggle.SetIsOnWithoutNotify(false);
        }

        UpdateSpeedLabel(Time.timeScale);
        LogState($"SetSpeedFromSlider(applied, value={value:0.###})");
    }

    void UpdateSpeedLabel(float value)
    {
        if (speedLabel) speedLabel.text = $"Sim Speed: {value:0.0}x";
    }

    void LogState(string source)
    {
        if (!logStateChanges) return;

        bool pausedOn = pauseToggle && pauseToggle.isOn;
        float sliderValue = speedSlider ? speedSlider.value : -1f;
        bool menuActive = menuRoot && menuRoot.activeSelf;

        Debug.Log(
            $"[DebugMenu] {source} | isOpen={isOpen} menuActive={menuActive} " +
            $"timeScale={Time.timeScale:0.###} lastNonZero={lastNonZeroScale:0.###} " +
            $"pauseToggle={pausedOn} slider={sliderValue:0.###} " +
            $"cursorLock={Cursor.lockState} cursorVisible={Cursor.visible}"
        );
    }

    void ApplyCursorState()
    {
        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
