using UnityEngine;
using UnityEngine.InputSystem;

public class StandardController : MonoBehaviour
{
    private NesEmulator emulator;
    public void Left(InputAction.CallbackContext context)
    {
        if (emulator != null)
        {
            emulator.controllerOne.Input(Controller.Button.Left, context.phase != InputActionPhase.Canceled);
        }
    }
    public void Right(InputAction.CallbackContext context)
    {
        if (emulator != null)
        {
            emulator.controllerOne.Input(Controller.Button.Right, context.phase != InputActionPhase.Canceled);
        }
    }

    public void Up(InputAction.CallbackContext context)
    {
        if (emulator != null)
        {
            emulator.controllerOne.Input(Controller.Button.Up, context.phase != InputActionPhase.Canceled);
        }
    }

    public void Down(InputAction.CallbackContext context)
    {
        if (emulator != null)
        {
            emulator.controllerOne.Input(Controller.Button.Down, context.phase != InputActionPhase.Canceled);
        }
    }

    public void A(InputAction.CallbackContext context)
    {
        if (emulator != null)
        {
            emulator.controllerOne.Input(Controller.Button.A, context.phase != InputActionPhase.Canceled);
        }
    }

    public void B(InputAction.CallbackContext context)
    {
        if (emulator != null)
        {
            emulator.controllerOne.Input(Controller.Button.B, context.phase != InputActionPhase.Canceled);
        }
    }

    public void StartButton(InputAction.CallbackContext context)
    {
        if (emulator != null)
        {
            emulator.controllerOne.Input(Controller.Button.Start, context.phase != InputActionPhase.Canceled);
        }
    }

    public void SelectButton(InputAction.CallbackContext context)
    {
        if (emulator != null)
        {
            emulator.controllerOne.Input(Controller.Button.Select, context.phase != InputActionPhase.Canceled);
        }
    }

    public void StartController(NesEmulator newEmulator)
    {
        emulator = newEmulator;
    }
}
