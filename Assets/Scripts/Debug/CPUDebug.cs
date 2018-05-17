using Nes;
using System.Text;
using TMPro;
using UnityEngine;

namespace NESEmulator
{
    public class CPUDebug : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI registers = default;
        [SerializeField]
        private TextMeshProUGUI cycles = default;

        private StringBuilder builder;

        private Cpu cpu;

        void Start()
        {
            builder = new StringBuilder();
        }

        void Update()
        {
            if (cpu == null)
            {
                return;
            }

            builder.Clear();

            builder.AppendLine($"PC: ${cpu.Pc:X4}");
            builder.AppendLine($"A:  ${cpu.A:X2} [{cpu.A}]");
            builder.AppendLine($"X:  ${cpu.X:X2} [{cpu.X}]");
            builder.AppendLine($"Y:  ${cpu.Y:X2} [{cpu.Y}]");
            builder.AppendLine($"S:  ${cpu.S:X2} [{cpu.S}]");
            builder.AppendLine($"P:  ${(byte)cpu.P:X2}");

            if (cpu.P.HasFlag(Cpu.Status.Negative))
            {
                builder.Append("<color=green>N </color>");
            }
            else
            {
                builder.Append("<color=red>N </color>");
            }
            if (cpu.P.HasFlag(Cpu.Status.Overflow))
            {
                builder.Append("<color=green>V </color>");
            }
            else
            {
                builder.Append("<color=red>V </color>");
            }
            builder.Append("- ");
            if (cpu.P.HasFlag(Cpu.Status.Bit4))
            {
                builder.Append("<color=green>B </color>");
            }
            else
            {
                builder.Append("<color=red>B </color>");
            }
            if (cpu.P.HasFlag(Cpu.Status.DecimalMode))
            {
                builder.Append("<color=green>D </color>");
            }
            else
            {
                builder.Append("<color=red>D </color>");
            }
            if (cpu.P.HasFlag(Cpu.Status.InterruptDisabled))
            {
                builder.Append("<color=green>I </color>");
            }
            else
            {
                builder.Append("<color=red>I </color>");
            }
            if (cpu.P.HasFlag(Cpu.Status.Zero))
            {
                builder.Append("<color=green>Z </color>");
            }
            else
            {
                builder.Append("<color=red>Z </color>");
            }
            if (cpu.P.HasFlag(Cpu.Status.Carry))
            {
                builder.Append("<color=green>C</color>");
            }
            else
            {
                builder.Append("<color=red>C</color>");
            }

            registers.text = builder.ToString();

            cycles.text = $"Cycles:{cpu.TotalCycles}";
        }

        public void StartCpuDebug(Cpu newCpu)
        {
            cpu = newCpu;
        }
    }
}
