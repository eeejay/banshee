//
// DeviceCommand.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using Banshee.Base;

namespace Banshee.Hardware
{
    public delegate void DeviceCommandHandler (object o, DeviceCommand args);

    [Flags]
    public enum DeviceCommandAction
    {
        None = (0 << 0),
        Activate = (1 << 0),
        Play = (1 << 1)
    }

    public sealed class DeviceCommand : EventArgs
    {
        private DeviceCommandAction action;
        public DeviceCommandAction Action {
            get { return action; }
        }

        private string device_id;
        public string DeviceId {
            get { return device_id; }
        }

        private DeviceCommand (DeviceCommandAction action)
        {
            this.action = action;
        }

        internal static DeviceCommand ParseCommandLine (string command, string argument)
        {
            if (command == null || argument == null || !command.StartsWith ("device")) {
                return null;
            }

            DeviceCommand d_command = null;
            
            switch (command) {
                case "device-activate-play":
                    d_command = new DeviceCommand (DeviceCommandAction.Activate | DeviceCommandAction.Play);
                    break;
                case "device-activate":
                    d_command = new DeviceCommand (DeviceCommandAction.Activate);
                    break;
                case "device-play":
                    d_command = new DeviceCommand (DeviceCommandAction.Play);
                    break;
                case "device":
                    d_command = new DeviceCommand (DeviceCommandAction.None);
                    break;
                default:
                    return null;
            }

            // FIXME: Should use GIO here to resolve UNIX device nodes or 
            // HAL UDIs from the GIO URI that we were likely given by Nautilus

            try {
                Banshee.Base.SafeUri uri = new Banshee.Base.SafeUri (argument);
                d_command.device_id = uri.IsLocalPath ? uri.LocalPath : argument;
            } catch {
                d_command.device_id = argument;
            }

            Hyena.Log.InformationFormat ("Received device command: action = {0}, device = {1}",
                d_command.Action, d_command.DeviceId);

            return d_command;
        }
    }
}

