//
// RemoteSpeaker.cs
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

namespace Banshee.RemoteAudio
{
    public class RemoteSpeaker
    {
        private string name;
        public string Name {
            get { return name; }
        }

        private IPAddress host;
        public IPAddress Host {
            get { return host; }
        }

        private short port;
        public short Port {
            get { return port; }
        }

        private string version;
        public string Version {
            get { return version; }
        }

        private int sample_rate;
        public int SampleRate {
            get { return sample_rate; }
        }

        private int sample_size;
        public int SampleSize {
            get { return sample_size; }
        }

        private int channels;
        public int Channels {
            get { return channels; }
        }

        public static RemoteSpeaker None
            = new RemoteSpeaker ("None", IPAddress.None, 0, String.Empty,
                                 0, 0, 0);

        internal RemoteSpeaker (string name, IPAddress host, short port, string version,
            int sample_rate, int sample_size, int channels)
        {
            this.name = name;
            this.host = host;
            this.port = port;
            this.version = version;
            this.sample_rate = sample_rate;
            this.sample_size = sample_size;
            this.channels = channels;
        }
    }
}
