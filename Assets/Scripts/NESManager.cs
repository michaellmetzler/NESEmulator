using UnityEngine;
using System.Diagnostics;

public class NESManager : MonoBehaviour
{
    [SerializeField]
    private NESScreen screen;

    [SerializeField]
    private StandardController controller;

    [SerializeField]
    private CPUDebug cpuDebug;

    [SerializeField]
    private PPUDebug ppuDebug;

    [SerializeField]
    private Log log;

    [SerializeField]
    private PaletteScriptableObject palletteSO;
    public Color32[] Palette => palletteSO.palette;

    [SerializeField]
    private bool stepMode;

    public NesEmulator Emulator { get; private set; }

    private Stopwatch stopWatch;
    private float nextUpdate;

    private void Awake()
    {
        stopWatch = new Stopwatch();
    }

#if !UNITY_EDITOR
    private void Start()
    {
        StartEmulator("D:/Projects/NESEmulator/Roms/donkey kong.nes");
    }
#endif

    private void Update()
    {
        if (Emulator == null || !Emulator.IsValid)
        {
            return;
        }

        nextUpdate -= Time.deltaTime;

        if (nextUpdate > 0)
        {
            return;
        }

        stopWatch.Restart();

        Emulator.Tick();

        stopWatch.Stop();

        nextUpdate = (float)((1f / NesEmulator.ApuFrameCounterRate) - stopWatch.Elapsed.TotalSeconds);
    }

    private void OnValidate()
    {
        UpdateStepMode();
    }

    private void OnDestroy()
    {
        StopEmulator();
    }

    public void StartEmulator(string path)
    {
        var cartridge = new Cartridge(path);
        if (!cartridge.IsValid)
        {
            return;
        }

        Emulator = new NesEmulator(cartridge);
        if (!Emulator.IsValid)
        {
            return;
        }

        UpdateStepMode();

        // Required Components
        screen.StartOutput(this);
        controller.StartController(Emulator);

        // Debug Components
        if (cpuDebug != null)
        {
            cpuDebug.StartCpuDebug(Emulator);
        }
        if (ppuDebug != null)
        {
            ppuDebug.StartPpuDebug(this);
        }
        if (log != null)
        {
            log.StartLog(Emulator);
        }

        Emulator.Init();
    }

    private void UpdateStepMode()
    {
        Emulator?.StepMode(stepMode);
    }

    public void StepEmulator()
    {
        Emulator?.Step();
    }

    public void ResetEmulator()
    {
        Emulator?.Reset();
    }

    public void StopEmulator()
    {
        Emulator?.Stop();
    }
}
