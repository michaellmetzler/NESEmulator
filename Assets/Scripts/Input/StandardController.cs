using Nes;
using UnityEngine;

namespace NESEmulator
{
    public class StandardController : MonoBehaviour
    {
        private Emulator _emulator;

        private void Update()
        {
            if(_emulator is null)
            {
                return;
            }

            _emulator.ControllerOne.Input(Controller.Button.Left, Input.GetKey(KeyCode.A));
            _emulator.ControllerOne.Input(Controller.Button.Right, Input.GetKey(KeyCode.D));
            _emulator.ControllerOne.Input(Controller.Button.Up, Input.GetKey(KeyCode.W));
            _emulator.ControllerOne.Input(Controller.Button.Down, Input.GetKey(KeyCode.S));
            _emulator.ControllerOne.Input(Controller.Button.A, Input.GetKey(KeyCode.I));
            _emulator.ControllerOne.Input(Controller.Button.B, Input.GetKey(KeyCode.U));
            _emulator.ControllerOne.Input(Controller.Button.Start, Input.GetKey(KeyCode.Return));
            _emulator.ControllerOne.Input(Controller.Button.Select, Input.GetKey(KeyCode.Backspace));
        }

        public void StartController(Emulator newEmulator)
        {
            _emulator = newEmulator;
        }
    }
}
