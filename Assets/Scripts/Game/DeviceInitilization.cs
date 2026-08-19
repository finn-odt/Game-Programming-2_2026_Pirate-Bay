using System.Collections.Generic;
using GameEvents;
using TriInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class DeviceInitialization : MonoBehaviour
{
    [SerializeField, LabelText("Input Action Asset - Player")] private InputActionAsset actionMap;

    private void Start()
    {
        SetupInputs();

        // Add Event for Device Changing (during runtime)
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }
    
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is not Gamepad)
            return;

        // if device is gamepad and was added/removed/...
        if (change == InputDeviceChange.Added ||
            change == InputDeviceChange.Removed ||
            change == InputDeviceChange.Disconnected ||
            change == InputDeviceChange.Reconnected)
        {
            SetupInputs();
        }
    }

    private void SetupInputs()
    {
        int pads = Gamepad.all.Count;

        //actionMap.Disable();

        if (pads == 0)
        {
            //actionMap.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
            //actionMap.devices = GetKeyboardMouseDevices();
        
            GameEventManager.Raise(new ConnectionModeChangedEvent(DeviceConnectionMode.Keyboard));
        }
        else
        {
            //actionMap.bindingMask = InputBinding.MaskByGroup("Gamepad");
            //actionMap.devices = new InputDevice[] { Gamepad.all[0] };
            
            GameEventManager.Raise(new ConnectionModeChangedEvent(DeviceConnectionMode.Gamepad));
        }

        //actionMap.Enable();
    }

    private InputDevice[] GetKeyboardMouseDevices()
    {
        var devices = new List<InputDevice>();

        if (Keyboard.current != null)
            devices.Add(Keyboard.current);

        if (Mouse.current != null)
            devices.Add(Mouse.current);

        return devices.ToArray();
    }
}