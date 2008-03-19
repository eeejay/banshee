//
// NullPlayerEngine.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Collections;

using Banshee.Base;

namespace Banshee.MediaEngine
{
    public class NullPlayerEngine : MediaEngine.PlayerEngine
    {
        protected override void OpenUri (SafeUri uri)
        {
        }
        
        private ushort volume;
        public override ushort Volume {
            get { return volume; }
            set { volume = value; }
        }
        
        public override uint Position {
            get { return 0; }
            set { return; }
        }
        
        public override bool CanSeek {
            get { return false; }
        }
        
        public override uint Length {
            get { return 0; }
        }
        
        public override bool SupportsEqualizer {
            get { return false; }
        }
        
        public override bool SupportsVideo {
            get { return false; }
        }
        
        private static string [] source_capabilities = { "file", "http", "cdda" };
        public override IEnumerable SourceCapabilities {
            get { return source_capabilities; }
        }
        
        public override IEnumerable ExplicitDecoderCapabilities {
            get { return new string[0]; }
        }
        
        public override string Id {
            get { return "nullplayerengine"; }
        }
        
        public override string Name {
            get { return "Null Player Engine"; }
        }
        
    }
}
