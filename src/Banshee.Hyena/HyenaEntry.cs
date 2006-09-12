using System;

using Banshee.Base;

namespace Banshee.Hyena
{
    public static class HyenaEntry
    {
        public static void Main(string [] args)
        {
            Banshee.Gui.CleanRoomStartup.Startup(Startup, args);
        }
        
        private static void Startup(string [] args)
        {
            try {
                Utilities.SetProcessName("hyena");
            } catch {}
            
            Gtk.Application.Init();
            
            Globals.ArgumentQueue = new ArgumentQueue(new ArgumentLayout[0], args, "enqueue");
            Globals.Initialize();
            
            HyenaUserInterface user_interface = new HyenaUserInterface();
            user_interface.Init();
        }
    }
}
