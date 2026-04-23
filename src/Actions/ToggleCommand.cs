namespace Loupedeck.LitraGlowCCPlugin.Actions
{
    using System;

    public class ToggleCommand : ActionEditorCommand
    {
        // Initializes the command class.
        public ToggleCommand()
        {
            this.Name = "ToggleCommand";
            this.Description = "Toggles the state of the selected Litra Glow devices.";
            this.GroupName = "Commands";
            this.DisplayName = "Toggle Lights";

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
                    LitraDriver.Toggle(device);
                }
            }
            return true;
        }
    }
}
