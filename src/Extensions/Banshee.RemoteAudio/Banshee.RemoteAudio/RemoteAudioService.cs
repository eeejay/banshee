//
// RemoteAudioService.cs
//
// Authors:
//   Brad Taylor <brad@getcoded.net>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Net;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Mono.Zeroconf;

using Hyena;
using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Networking;

namespace Banshee.RemoteAudio
{
    public class RemoteAudioService : IExtensionService, IDisposable
    {
        private const string RAOP_MDNS_TYPE = "_raop._tcp";

        private ServiceBrowser browser;
        private List<RemoteSpeaker> speakers = new List<RemoteSpeaker> ();

        public event EventHandler SpeakersChanged;

        public RemoteAudioService ()
        {
        }

        public ReadOnlyCollection<RemoteSpeaker> Speakers {
            get { lock (speakers) { return speakers.AsReadOnly (); } }
        }

        void IExtensionService.Initialize ()
        {
            Network network = ServiceManager.Get<Network> ();
            network.StateChanged += OnNetworkStateChanged;

            if (network.Connected) {
                Browse ();
            }

            // TODO: move this to another service
            RemoteAudioActions a = new RemoteAudioActions ();
            a.Register ();
        }

        public void Dispose ()
        {
            if (browser != null) {
                browser.Dispose ();
            }
        }

        string Banshee.ServiceStack.IService.ServiceName {
            get { return "RemoteAudioService"; }
        }

        private void Browse ()
        {
            browser = new ServiceBrowser ();
            browser.ServiceAdded += OnServiceAdded;
            browser.ServiceRemoved += OnServiceRemoved;
            browser.Browse (0, AddressProtocol.Any, RAOP_MDNS_TYPE, "local");
        }

        private void OnNetworkStateChanged (object o, NetworkStateChangedArgs args)
        {
            if (!args.Connected) {
                browser.Dispose ();
                browser = null;
                return;
            }

            Browse ();
        }

        private void OnServiceAdded (object o, ServiceBrowseEventArgs args)
        {
            Log.Debug ("Found RAOP service...");
            args.Service.Resolved += OnServiceResolved;
            args.Service.Resolve ();
        }

        private void OnServiceRemoved (object o, ServiceBrowseEventArgs args)
        {
            // TODO: remove service
        }

        private void OnServiceResolved (object o, ServiceResolvedEventArgs args)
        {
            IResolvableService service = o as IResolvableService;
            if (service == null) {
                return;
            }

            Log.DebugFormat ("Resolved RAOP service at {0}", service.HostEntry.AddressList[0]);

            ITxtRecord record = service.TxtRecord;

            string version = String.Empty;
            int sample_size = 16, sample_rate = 44100, channels = 2;

            for (int i = 0, n = record.Count; i < n; i++) {
                TxtRecordItem item = record.GetItemAt(i);
                switch (item.Key) {
                    case "vs":
                        version = item.ValueString;
                        break;
                    case "ss":
                        sample_size = Convert.ToInt32 (item.ValueString);
                        break;
                    case "sr":
                        sample_rate = Convert.ToInt32 (item.ValueString);
                        break;
                    case "ch":
                        channels = Convert.ToInt32 (item.ValueString);
                        break;
                }
            }

            lock (speakers) {
                // TODO: better Name
                speakers.Add (new RemoteSpeaker (service.HostEntry.HostName,
                    service.HostEntry.AddressList[0], service.Port,
                    version, sample_rate, sample_size, channels));
            }

            EventHandler handler = SpeakersChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
    }
}
