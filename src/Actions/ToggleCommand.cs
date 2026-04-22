namespace Loupedeck.LitraGlowCCPlugin.Actions
{
    using System;

    public class ToggleCommand : ActionEditorCommand
    {
        // Initializes the command class.
        public ToggleCommand()
        {
            this.Name = "ToggleLights";
            this.Description = "Toggles the state of the selected Litra Glow devices.";
            this.GroupName = "Commands";
            this.DisplayName = "Toggle Lights";

            var lights = LitraDriver.FindDevices();
            foreach (Device device in lights)
            {
                this.ActionEditor.AddControlEx(new ActionEditorCheckbox("checkbox_" + device.SerialNumber, device.Type.ToString() + " - " + device.SerialNumber));
            }
        }

        // This method is called when the user executes the command.
        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            var devices = LitraDriver.FindDevices();
            foreach (Device device in devices)
            {
                if (actionParameters.GetString("checkbox_" + device.SerialNumber) == "true")
                {
                    LitraDriver.Toggle(device);
                }
            }
            return true;
        }
    }
}
