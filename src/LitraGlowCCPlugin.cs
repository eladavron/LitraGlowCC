namespace Loupedeck.LitraGlowCCPlugin
{
    using System;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class LitraGlowCCPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Initializes a new instance of the plugin class.
        public LitraGlowCCPlugin()
        {
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
        }
    }
}
