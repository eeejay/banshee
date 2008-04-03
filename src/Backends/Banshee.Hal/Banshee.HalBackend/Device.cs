//
// Device.cs
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
    public abstract class Device : IDevice
    {    
        private Hal.Device device;
        internal Hal.Device HalDevice {
            get { return device; }
        }
        
        private Hal.Manager manager;
        protected Hal.Manager HalManager {
            get { return manager; }
        }

        protected Device (Hal.Manager manager, Hal.Device device)
        {
            this.manager = manager;
            this.device = device;
        }
        
        public virtual string Uuid {
            get { return device.Udi; }
        }

        private string name;
        public virtual string Name {
            get {
                if (name == null) {
                    name = device["info.product"];
                    if (String.IsNullOrEmpty (name)) {
                        name = device["volume.label"];
                    }
                }

                return name;
            }
        }
    }
}
