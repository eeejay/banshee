//
// DiscVolume.cs
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

using Banshee.Hardware;

namespace Banshee.HalBackend
{
    public class DiscVolume : Volume, IDiscVolume
    {
        public new static DiscVolume Resolve (BlockDevice parent, Hal.Manager manager, Hal.Device device)
        {
            return device.QueryCapability ("volume.disc") ? new DiscVolume (parent, manager, device) : null;
        }
        
        private DiscVolume (BlockDevice parent, Hal.Manager manager, Hal.Device device) : base (parent, manager, device)
        {
        }
        
        public bool HasAudio {
            get { return HalDevice.GetPropertyBoolean ("volume.disc.has_audio"); }
        }
        
        public bool HasData {
            get { return HalDevice.GetPropertyBoolean ("volume.disc.has_data"); }
        }
        
        public bool IsBlank {
            get { return HalDevice.GetPropertyBoolean ("volume.disc.is_blank"); }
        }
        
        public bool IsRewritable {
            get { return HalDevice.GetPropertyBoolean ("volume.disc.is_rewritable"); }
        }
        
        public ulong MediaCapacity {
            get { return HalDevice.GetPropertyUInt64 ("volume.disc.capacity"); }
        }
        
        public override string ToString ()
        {
            return String.Format ("Optical Disc, Audio = {0}, Data = {1}, Blank = {2}, Rewritable = {3}, Media Capacity = {4}",
                HasAudio, HasData, IsBlank, IsRewritable, MediaCapacity);
        }
    }
}
