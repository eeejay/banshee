//
// TorrentService.cs
//
// Author:
//   Alan McGovern <alan.mcgovern@gmail.com>
//
// Copyright (C) 2008 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;
using MonoTorrent.DBus;

using Hyena;

using Banshee.ServiceStack;

namespace Banshee.Torrent
{
    public class TorrentService : IExtensionService, IDelayedInitializeService
    {
        static bool RegisteredInMigo = false;
        public static readonly string BusName = "org.monotorrent.dbus";
        public static readonly string EngineName = "banshee";
        public static readonly ObjectPath ServicePath = new ObjectPath ("/org/monotorrent/service");
        
        private Bus bus;
        private IEngine engine;
        private ITorrentService service;
        private IEngineSettings settings;
        
        public int MaxDownloadSpeed {
            get { return settings.GlobalMaxDownloadSpeed; }
            set { settings.GlobalMaxDownloadSpeed = value; }
        }
        
        public int MaxUploadSpeed {
            get { return settings.GlobalMaxUploadSpeed; }
            set { settings.GlobalMaxUploadSpeed = value; }
        }

        public string ServiceName {
            get { return "TorrentService"; }
        }
        
        public TorrentService ()
        {
        }
        
        public IDownloader Download (string torrentUri, string savePath)
        {
            // Get the associated downloader
            ObjectPath path = engine.RegisterTorrent (torrentUri, savePath);
            IDownloader downloader = bus.GetObject <IDownloader> (BusName, path);
            
            if (downloader.State == TorrentState.Stopped) {
                downloader.Start ();
                Console.WriteLine ("Started: {0}", downloader.Path);
            } else {
                Console.WriteLine ("{0} already running", downloader.Path);
            }
            return downloader;
        }
        
        public void Dispose ()
        {
            if (service != null) {
                service.DestroyEngine (EngineName);
                service = null;
            }
        }

        public void Initialize ()
        {
        }

        public void DelayedInitialize ()
        {
            bus = Bus.Session;
                        
            try {
                // Get the service and call a method on it to ensure that it is
                // running and able to answer queries.
                service = bus.GetObject<ITorrentService> (BusName, ServicePath);
                service.AvailableEngines ();
            } catch {
                Log.Error ("Torrent backend could not be found and could not be auto-started");
                service = null;
                return;
            }
            
            // Register with Migo so we can handle .torrent downloads
            Migo.DownloadCore.DownloadManager.Register ("torrent", typeof (TorrentFileDownloadTask));
            RegisteredInMigo = true;
            
            // Get the engine from DBus which we will use to download torrents with
            // and load the details for any existing downloads
            engine = bus.GetObject <IEngine> (BusName, service.GetEngine (EngineName));
            CheckExistingDownloads ();
        }
        
        private void CheckExistingDownloads ()
        {
            //UserJobManager manager = (UserJobManager)ServiceManager.Get ("UserJobManager");
            ObjectPath[] downloaders = engine.GetDownloaders ();
            foreach (ObjectPath o in downloaders)
            {
                Console.WriteLine ("Existing download: {0}", o);
                //IDownloader downloader = this.bus.GetObject<IDownloader> (BusName, o);
                //ITorrent torrent = this.bus.GetObject<ITorrent> (BusName, downloader.Torrent);
                //manager.Register (new DownloaderJob (torrent.Name, downloader));
                //if (downloader.State == TorrentState.Stopped)
                //    downloader.Start ();
            }
        }
    }
}
