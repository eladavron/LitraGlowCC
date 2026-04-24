namespace Loupedeck.LitraGlowCCPlugin
{
    using System;

    // This class implements an example adjustment that counts the rotation ticks of a dial.

    public class TemperatureAdjustment : ActionEditorAdjustment
    {
        public TemperatureAdjustment() : base(false)
        {
            this.Name = "TemperatureAdjustment";
            this.Description = "Adjust the temperature of the Litra lights by rotating the dial.";
            this.GroupName = "Adjustments";
            this.DisplayName = "Temperature";
            Helpers.Utils.CreateDeviceToggles(this);
        }


        protected override Boolean ApplyAdjustment(ActionEditorActionParameters actionParameters, Int32 diff)
        {
            var devices = LitraDriver.FindDevices();
            foreach (Device device in devices)
            {
                if (actionParameters.GetString(device.SerialNumber) == "true")
                {
                    var currentTemperature = LitraDriver.GetTemperatureInPercent(device);
                    var newTemperature = currentTemperature + diff;
                    newTemperature = Math.Clamp(newTemperature, 0, 100);
                    LitraDriver.SetTemperatureInPercent(device, newTemperature);
                }
            }
            return true;
        }
    }
}
