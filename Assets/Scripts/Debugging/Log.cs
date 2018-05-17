using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using UnityEngine;

public class Log : MonoBehaviour
{
    public bool enableLog;

    private ConcurrentQueue<string> queue;

    private string path;

    private NesEmulator emulator;
    private Cpu cpu;
    private Ppu ppu;

    // Thread
    private Thread logThread;

    private void Start()
    {
        queue = new ConcurrentQueue<string>();

        path = Path.Combine(Application.dataPath, "Log.txt");
    }

    private void OnEnable()
    {
        if (emulator != null)
        {
            emulator.OnStep -= QueueLog;
            emulator.OnStep += QueueLog;
        }
    }

    private void OnDisable()
    {
        if (emulator != null)
        {
            emulator.OnStep -= QueueLog;
        }
    }

    private void OnApplicationQuit()
    {
        logThread?.Abort();
    }

    public void StartLog(NesEmulator newEmulator)
    {
        emulator = newEmulator;
        cpu = emulator.cpu;
        ppu = emulator.ppu;

        if (enableLog)
        {
            // Add callback
            emulator.OnStep -= QueueLog;
            emulator.OnStep += QueueLog;

            // Clear queue
            while (queue?.TryDequeue(out _) ?? false) ;

            // Clear log
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            // Start Thread
            logThread = new Thread(() => { this.MessageLoop(); });
            logThread.Start();
        }
    }

    private void MessageLoop()
    {
        using (StreamWriter outputFile = new StreamWriter(path, true))
        {
            while (true)
            {
                if (queue.TryDequeue(out string result))
                {
                    outputFile.WriteLine(result);
                }
            }
        }
    }

    private void QueueLog()
    {
        var opcode = cpu.ReadMemoryByte(cpu.PC);
        var opcodeToExecute = cpu.opcodeLookup[opcode];

        string opcodeString = null;
        if (opcodeToExecute.OpcodeSize == 1)
        {
            opcodeString = $"{opcode:X2}       ";
        }
        else if (opcodeToExecute.OpcodeSize == 2)
        {
            opcodeString = $"{opcode:X2} {cpu.ReadMemoryByte((ushort)(cpu.PC + 1)):X2}    ";
        }
        else if (opcodeToExecute.OpcodeSize == 3)
        {
            opcodeString = $"{opcode:X2} {cpu.ReadMemoryByte((ushort)(cpu.PC + 1)):X2} {cpu.ReadMemoryByte((ushort)(cpu.PC + 2)):X2} ";
        }

        var message =($"{cpu.PC:X4}  {opcodeString} {opcodeToExecute.Instruction.Method.Name:D3}                             " +
                      $"A:{cpu.A:X2} X:{cpu.X:X2} Y:{cpu.Y:X2} P:{(byte)(cpu.P):X2} SP:{cpu.S:X2} " +
                      $"PPU:{ppu.CurrentCycle},{ppu.CurrentScanline} " +
                      $"CYC:{emulator.cpu.TotalCycles}");

        queue.Enqueue(message);
    }
}
