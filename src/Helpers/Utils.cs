namespace Loupedeck.LitraGlowCCPlugin.Helpers
{
    public static class Utils
    {
        public static void CreateDeviceToggles(ActionEditorAction action)
        {
            var lights = LitraDriver.FindDevices();
            var devices = new Dictionary<String, Device>();
            foreach (Device device in lights)
            {
                devices[device.SerialNumber] = device;
                ActionEditorCheckbox checkbox = new(device.SerialNumber, device.Type.ToString());
                action.ActionEditor.AddControlEx(checkbox);
            }
            action.ActionEditor.ControlValueChanged += (sender, e) =>
            {
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
                LitraDriver.Toggle(device);
                Thread.Sleep(delay);
                LitraDriver.Toggle(device);
                if (i < times - 1)
                {
                    Thread.Sleep(delay);
                }
            }
        }
    }
}