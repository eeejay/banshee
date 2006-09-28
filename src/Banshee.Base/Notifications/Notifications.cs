/*
 * Copyright (c) 2006 Sebastian Dröge <slomo@circular-chaos.org>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop;
using org.freedesktop.DBus;

namespace org.freedesktop.Notifications {
	public struct ServerInformation {
		public string Name;
		public string Vendor;
		public string Version;
		public string SpecVersion;
	}

	[Interface ("org.freedesktop.Notifications")]
	public interface INotifications : Introspectable, Properties {
		ServerInformation ServerInformation { get; }
		string[] Capabilities { get; }
		void CloseNotification (uint id);
		uint Notify (string app_name, uint id, string icon, string summary, string body,
			string[] actions, IDictionary<string, object> hints, int timeout);
		event NotificationClosedHandler NotificationClosed;
		event ActionInvokedHandler ActionInvoked;
	}

	public delegate void NotificationClosedHandler (uint id);
	public delegate void ActionInvokedHandler (uint id, string action);
}

