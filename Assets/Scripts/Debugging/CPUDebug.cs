using System.Text;
using TMPro;
using UnityEngine;

public class CPUDebug : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI registers = default;
    [SerializeField]
    private TextMeshProUGUI cycles = default;

    StringBuilder builder;

    private NesEmulator emulator;
    private Cpu cpu;

    private void Start()
    {
        builder = new StringBuilder();
    }

    private void Update()
    {
        if(registers != null && cpu != null)
        {
            builder.Clear();

            builder.Append($"PC: ${cpu.PC:X4}\n");
            builder.Append($"A:  ${cpu.A:X2} [{cpu.A}]\n");
            builder.Append($"X:  ${cpu.X:X2} [{cpu.X}]\n");
            builder.Append($"Y:  ${cpu.Y:X2} [{cpu.Y}]\n");
            builder.Append($"S:  ${cpu.S:X2} [{cpu.S}]\n");
            builder.Append($"P:  ${(byte)(cpu.P):X2}\n");

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

            cycles.text = $"Cycles:{emulator.cpu.TotalCycles}";
        }
    }

    public void StartCpuDebug(NesEmulator newEmulator)
    {
        emulator = newEmulator;
        cpu = emulator.cpu;
    }
}
