namespace Loupedeck.LitraGlowCCPlugin.Actions
{
    using System;

    public class TemperatureCommand : ActionEditorCommand
    {
        // Initializes the command class.
        public TemperatureCommand()
        {
            this.Name = "TemperatureCommand";
            this.Description = "Set the temperature of the Litra device.";
            this.GroupName = "Commands";
            this.DisplayName = "Set Temperature";
            var slider = new ActionEditorSlider("temperature", "Temperature (%)", "The temperature in % to set the light to. Lower is warmer light.");
            slider.SetValues(1, 100, 50, 1);
            this.ActionEditor.AddControlEx(slider);
            this.ActionEditor.ControlValueChanged += (sender, e) =>
            {
                if (e.ControlName == "temperature")
                {
                    var value = e.ActionEditorState.GetControlValue("temperature");
                    e.ActionEditorState.SetDisplayName($"{value}% Temperature");
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
                    LitraDriver.SetTemperatureInPercent(device, actionParameters.GetInt32("temperature"));
                }
            }
            return true;
        }
    }
}
