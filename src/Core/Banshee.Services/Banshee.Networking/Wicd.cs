//
// Wicd.cs
//
// Author:
//   Jérémie Laval <jeremie.laval@gmail.com>
//
// Copyright 2009 Jérémie "Garuma" Laval
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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NDesk.DBus;

namespace Banshee.Networking
{
    public class Wicd : INetworkAvailabilityService
    {
        public delegate void StateChangeInternalHandler (uint code, object [] foo);

        internal struct StatusStruct
        {
            public uint Code;
            public string [] Foo;
        }

        [Interface ("org.wicd.daemon")]
        internal interface IWicd
        {
            event StateChangeInternalHandler StatusChanged;
            StatusStruct GetConnectionStatus ();
        }

        private enum WicdCodes : uint {
            NotConnected = 0,
            Connecting,
            Wired,
            Wireless,
            Suspended
        }

        private const string BusName = "org.wicd.daemon";
        private const string ObjectPath = "/org/wicd/daemon";

        private IWicd manager;

        public event StateChangeHandler StateChange;

        public Wicd ()
        {
            if (!ManagerPresent) {
                throw new ApplicationException (String.Format ("Name {0} has no owner", BusName));
            }

            manager = Bus.System.GetObject<IWicd> (BusName, new ObjectPath(ObjectPath));
            manager.StatusChanged += OnStateChange;
        }

        private void OnStateChange (uint code, object[] foo)
        {
            var handler = StateChange;
            if (handler != null) {
                handler(GetTranslatedCode (code));
            }
        }

        public State State {
            get { return GetTranslatedCode (manager.GetConnectionStatus ().Code); }
        }

        public static bool ManagerPresent {
            get { return Bus.System.NameHasOwner (BusName); }
        }

        private State GetTranslatedCode (uint wicdCode)
        {
            switch ((WicdCodes)wicdCode) {
                case WicdCodes.NotConnected:
                    return State.Disconnected;
                case WicdCodes.Connecting:
                    return State.Connecting;
                case WicdCodes.Wireless:
                case WicdCodes.Wired:
                    return State.Connected;
                case WicdCodes.Suspended:
                    return State.Asleep;
                default:
                    return State.Unknown;
            }
        }
    }
}
