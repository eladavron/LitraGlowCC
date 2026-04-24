namespace Loupedeck.LitraGlowCCPlugin.Actions
{
    using System;

    public class BrightnessCommand : ActionEditorCommand
    {
        // Initializes the command class.
        public BrightnessCommand()
        {
            this.Name = "BrightnessCommand";
            this.Description = "Set the brightness of the Litra device.";
            this.GroupName = "Commands";
            this.DisplayName = "Set Brightness";
            var slider = new ActionEditorSlider("brightness", "Brightness (%)", "The brightness percentage to set the light to");
            slider.SetValues(1, 100, 50, 1);
            this.ActionEditor.AddControlEx(slider);
            this.ActionEditor.ControlValueChanged += (sender, e) =>
            {
                if (e.ControlName == "brightness")
                {
                    var value = e.ActionEditorState.GetControlValue("brightness");
                    e.ActionEditorState.SetDisplayName($"{value}% Brightness");
                }
            };
            Helpers.Utils.CreateDeviceToggles(this);
        }

        // This method is called when the user executes the command.
        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            var devices = LitraDriver.FindDevices();
            foreach (Device device in devices)
            {
                if (actionParameters.GetString(device.SerialNumber) == "true")
                {
                    LitraDriver.SetBrightnessPercent(device, actionParameters.GetInt32("brightness"));
                }
            }
            return true;
        }
    }
}
