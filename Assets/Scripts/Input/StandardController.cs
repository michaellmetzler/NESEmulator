using Nes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NESEmulator
{
    public class StandardController : MonoBehaviour
    {
        private InputAction downAction;
        private InputAction leftAction;
        private InputAction rightAction;
        private InputAction upAction;
        private InputAction aAction;
        private InputAction bAction;
        private InputAction startAction;
        private InputAction selectAction;

        private Controller _controllerOne;
        private Controller _controllerTwo;

        private void Awake()
        { 
            downAction = InputSystem.actions.FindAction("Down");
            leftAction = InputSystem.actions.FindAction("Left");
            rightAction = InputSystem.actions.FindAction("Right");
            upAction = InputSystem.actions.FindAction("Up");
            aAction = InputSystem.actions.FindAction("A");
            bAction = InputSystem.actions.FindAction("B");
            startAction = InputSystem.actions.FindAction("Start");
            selectAction = InputSystem.actions.FindAction("Select");
        }
        
        private void Update()
        {
            if(_controllerOne is null)
            {
                return;
            }

            _controllerOne.Input(Controller.Button.Down, downAction.IsPressed());
            _controllerOne.Input(Controller.Button.Left, leftAction.IsPressed());
            _controllerOne.Input(Controller.Button.Right, rightAction.IsPressed());
            _controllerOne.Input(Controller.Button.Up, upAction.IsPressed());
            _controllerOne.Input(Controller.Button.A, aAction.IsPressed());
            _controllerOne.Input(Controller.Button.B, bAction.IsPressed());
            _controllerOne.Input(Controller.Button.Start, startAction.IsPressed());
            _controllerOne.Input(Controller.Button.Select, selectAction.IsPressed());
        }

        public void StartController(Controller controllerOne, Controller controllerTwo)
        {
            _controllerOne = controllerOne;
            _controllerTwo = controllerTwo;
        }
    }
}
