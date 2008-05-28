//
// DBusCommandService.cs
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
using NDesk.DBus;

namespace Banshee.ServiceStack
{
    public delegate void DBusCommandHandler (string argument, object value, bool isFile);

    [Interface ("org.bansheeproject.Banshee.CommandService")]
    public class DBusCommandService : MarshalByRefObject, IDBusExportable
    {
        public event DBusCommandHandler ArgumentPushed;
        
        public DBusCommandService ()
        {
        }
        
        public void PushArgument (string argument, object value)
        {
            OnArgumentPushed (argument, value, false);
        }
        
        public void PushFile (string file)
        {
            OnArgumentPushed (file, String.Empty, true);
        }
        
        private void OnArgumentPushed (string argument, object value, bool isFile)
        {
            DBusCommandHandler handler = ArgumentPushed;
            if (handler != null) {
                handler (argument, value, isFile);
            }
        }
        
        IDBusExportable IDBusExportable.Parent {
            get { return null; }
        }
        
        string IService.ServiceName {
            get { return "DBusCommandService"; }
        }
    }
}
