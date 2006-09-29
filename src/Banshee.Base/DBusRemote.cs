/***************************************************************************
 *  DBusRemote.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover (aaron@abock.org)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Banshee.Base
{
    public class DBusRemote
    {
        public const string BusName = "org.gnome.Banshee";
        public const string ObjectRoot = "/org/gnome/Banshee";

        public DBusRemote()
        {
            try {
                NameReply nameReply = Bus.Session.RequestName(BusName);
                // TODO: error handling based on nameReply. should probably throw if 
                // nameReply is anything other than NameReply.PrimaryOwner
            } catch(Exception e) {
                LogCore.Instance.PushWarning("Could not connect to D-Bus", 
                    "D-Bus support will be disabled for this instance: " + e.Message, false);
            }
        }
       
        public void RegisterObject(object o, string objectName)
        {
            if(Bus.Session != null) {
                Bus.Session.Register(BusName, new ObjectPath(ObjectRoot + "/" + objectName), o);
            }
        }
       
        public void UnregisterObject(object o)
        {
            //TODO: unregistering objects with managed dbus
        }
    }
}
