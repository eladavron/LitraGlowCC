namespace Loupedeck.LitraGlowCCPlugin
{
    using System;

    // This class implements an example adjustment that counts the rotation ticks of a dial.

    public class BrightnessAdjustment : ActionEditorAdjustment
    {
        public BrightnessAdjustment() : base(false)
        {
            this.Name = "BrightnessAdjustment";
            this.Description = "Adjust the brightness of the Litra lights by rotating the dial.";
            this.GroupName = "Adjustments";
            this.DisplayName = "Brightness";
            Helpers.Utils.CreateDeviceToggles(this);
        }


        protected override Boolean ApplyAdjustment(ActionEditorActionParameters actionParameters, Int32 diff)
        {
            var devices = LitraDriver.FindDevices();
            foreach (Device device in devices)
            {
                if (actionParameters.GetString(device.SerialNumber) == "true")
                {
                    var currBrightness = LitraDriver.GetBrightnessPercent(device);
                    var newBrightness = currBrightness + diff;
                    newBrightness = Math.Clamp(newBrightness, 0, 100);
                    LitraDriver.SetBrightnessPercent(device, newBrightness);
                }
            }
            return true;
        }
    }
}
