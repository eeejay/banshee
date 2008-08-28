//
// RemoteSpeakersComboBox.cs
//
// Author:
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
using System.Collections.Generic;

using Gtk;

using Banshee.Gui;
using Banshee.Base;
using Banshee.Widgets;
using Banshee.ServiceStack;
using Banshee.Sources;

namespace Banshee.RemoteAudio
{
    public class RemoteSpeakersComboBox : DictionaryComboBox<RemoteSpeaker>
    {
        private RemoteAudioService audio_svc;

        public RemoteSpeakersComboBox () : base ()
        {
            ServiceManager.ServiceStarted += OnServiceStarted;
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            audio_svc = args.Service as RemoteAudioService;
            if (audio_svc == null) {
                return;
            }

            audio_svc.SpeakersChanged += OnSpeakersChanged;
            ServiceManager.ServiceStarted -= OnServiceStarted;

            ThreadAssist.ProxyToMain(Refresh);
        }

        private void Refresh ()
        {
            // preserve the previous selection, if it exists
            RemoteSpeaker selected_speaker = ActiveValue;

            Clear ();

            Add ("None", RemoteSpeaker.None);

            foreach (RemoteSpeaker speaker in audio_svc.Speakers) {
                // TODO: speaker.Name may not be unique
                try {
                    Add (speaker.Name, speaker);
                } catch (Exception e) {
                    Hyena.Log.Exception (e);
                }
            }

            if (selected_speaker != null) {
                ActiveValue = selected_speaker;
            } else {
                ActiveValue = RemoteSpeaker.None;
            }
        }

        private void OnSpeakersChanged (object o, EventArgs args)
        {
            ThreadAssist.ProxyToMain (Refresh);
        }
    }
}
