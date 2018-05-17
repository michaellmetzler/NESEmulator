//using NES;
//using System.Collections.Concurrent;
//using System.IO;
//using System.Threading;
//using UnityEngine;

//namespace NESEmulator
//{
//    public class Log : MonoBehaviour
//    {
//        public bool enableLog;

//        private const string LogFilename = "Log.txt";

//        private ConcurrentQueue<string> queue;
//        private string path;
//        private Thread logThread;

//        private Emulator emulator;
//        private Cpu cpu;
//        private Ppu ppu;

//        private void Start()
//        {
//            queue = new ConcurrentQueue<string>();

//            path = Path.Combine(Application.dataPath, LogFilename);
//        }

//        private void OnEnable()
//        {
//            if (emulator != null)
//            {
//                emulator.UnregisterOnBeforeStep(QueueLog);
//                emulator.RegisterOnBeforeStep(QueueLog);
//            }
//        }

//        private void OnDisable()
//        {
//            if (emulator != null)
//            {
//                emulator.UnregisterOnBeforeStep(QueueLog);
//            }
//        }

//        private void OnApplicationQuit()
//        {
//            logThread?.Abort();
//        }

//        public void StartLog(Emulator newEmulator)
//        {
//            emulator = newEmulator;
//            cpu = emulator.cpu;
//            ppu = emulator.ppu;

//            if (enableLog)
//            {
//                // Add callback
//                emulator.UnregisterOnBeforeStep(QueueLog);
//                emulator.RegisterOnBeforeStep(QueueLog);

//                // Clear queue
//                while (queue.TryDequeue(out _)) ;

//                // Clear log
//                if (File.Exists(path))
//                {
//                    File.Delete(path);
//                }

//                // Start Thread
//                logThread = new Thread(() => { MessageLoop(); });
//                logThread.Start();
//            }
//        }

//        private void MessageLoop()
//        {
//            using var outputFile = new StreamWriter(path, true);
//            while (true)
//            {
//                if (queue.TryDequeue(out string result))
//                {
//                    outputFile.WriteLine(result);
//                }
//            }
//        }

//        private void QueueLog()
//        {
//            var opcode = cpu.ReadMemoryByte(cpu.Pc);
//            var opcodeToExecute = cpu.OpcodeLookup[opcode];

//            string opcodeString = null;
//            if (opcodeToExecute.opcodeSize == 1)
//            {
//                opcodeString = $"{opcode:X2}       ";
//            }
//            else if (opcodeToExecute.opcodeSize == 2)
//            {
//                opcodeString = $"{opcode:X2} {cpu.ReadMemoryByte((ushort)(cpu.Pc + 1)):X2}    ";
//            }
//            else if (opcodeToExecute.opcodeSize == 3)
//            {
//                opcodeString = $"{opcode:X2} {cpu.ReadMemoryByte((ushort)(cpu.Pc + 1)):X2} {cpu.ReadMemoryByte((ushort)(cpu.Pc + 2)):X2} ";
//            }

//            var message = $"{cpu.Pc:X4}  {opcodeString} {opcodeToExecute.instruction.Method.Name:D3}                             " +
//                          $"A:{cpu.A:X2} X:{cpu.X:X2} Y:{cpu.Y:X2} P:{(byte)cpu.P:X2} SP:{cpu.S:X2} " +
//                          $"PPU:{ppu.CurrentCycle},{ppu.CurrentScanline} " +
//                          $"CYC:{emulator.cpu.TotalCycles}";

//            queue.Enqueue(message);
//        }
//    }
//}
