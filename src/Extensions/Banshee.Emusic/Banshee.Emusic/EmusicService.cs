using System;
using System.IO;
using Mono.Unix;

using Hyena;

using Banshee.ServiceStack;
using Banshee.Library;
using Banshee.Gui;

namespace Banshee.Emusic
{
    public class EmusicService : IExtensionService, IDisposable
    {
        public EmusicService ()
        {
            Log.DebugFormat ("{0} constructed.", this.ToString());
        }

        public void Initialize ()
        {
            Log.DebugFormat ("{0} initialized.", this.ToString());
            ServiceManager.Get<DBusCommandService> ().ArgumentPushed += OnCommandLineArgument;
        }

        public void Dispose ()
        {
            Log.DebugFormat ("{0} disposed.", this.ToString());
        }

        string IService.ServiceName {
            get { return "EmusicService"; }
        }

        private void OnCommandLineArgument (string argument, object value, bool isFile)
        {
            if (isFile && File.Exists (argument) && Path.GetExtension (argument) == ".emx") {
                EmusicImport emusic_import = null;
                

                foreach (IImportSource importer in ServiceManager.Get<ImportSourceManager> ())
                    if (importer.Name == Catalog.GetString ("eMusic Tracks"))
                        emusic_import = importer as EmusicImport;

                if (emusic_import != null)
                    emusic_import.ImportEmx (argument);
            }
        }
    }
}