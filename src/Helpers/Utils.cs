namespace Loupedeck.LitraGlowCCPlugin.Helpers
{
    public static class Utils
    {
        public static void CreateDeviceToggles(ActionEditorCommand command)
        {
            var lights = LitraDriver.FindDevices();
            var devices = new Dictionary<String, Device>();
            foreach (Device device in lights)
            {
                devices[device.SerialNumber] = device;
                ActionEditorCheckbox checkbox = new(device.SerialNumber, device.Type.ToString());
                command.ActionEditor.AddControlEx(checkbox);
            }
            command.ActionEditor.ControlValueChanged += (sender, e) =>
            {
                PluginLog.Info("Updating checkbox state for device: " + e.ControlName);
                if (devices.TryGetValue(e.ControlName, out Device device))
                {
                    FlashDevice(device);
                }
            };
        }

        public static void FlashDevice(Device device, Int32 times = 2, Int32 delay = 750)
        {
            for (var i = 0; i < times; i++)
            {
                LitraDriver.TurnOn(device);
                Thread.Sleep(delay);
                LitraDriver.TurnOff(device);
                if (i < times - 1)
                {
                    Thread.Sleep(delay);
                }
            }
        }
    }
}