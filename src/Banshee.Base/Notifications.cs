/***************************************************************************
 *  Notifications.cs
 *
 *  Copyright (C) 2006 Ruben Vermeersch <ruben@savanne.be>
 * 
 *  Written by Ruben Vermeersch <ruben@savanne.be>
 *  
 *  Temporary hack to replace notify-sharp (as it's far from finished). Based
 *  on the code found in last-exit-2.0.
 *  
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

using Gtk;
using System;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Banshee.Base { 
    public static class Notifications {
        private static Gtk.Widget widget = null;

        // This widget will be used to position the notification bubble. This
        // is set to the tray icon by the tray icon plugin (if enabled).
        // Otherwise it is set to null, which causes the notification bubble to
        // come up on the lower right of the screen.
        public static Gtk.Widget Widget {
            get { return widget; }
            set { widget = value; }
        }

        public static void Notify (string summary,
                                   string message,
                                   Gdk.Pixbuf image) {
            try {
                if (!notify_init("Banshee")) {
                    throw new Exception("Failed to initialize");
                }

                IntPtr notification = notify_notification_new(summary, message, null, widget == null ? System.IntPtr.Zero : widget.Handle);
                notify_notification_set_timeout(notification, 4500);

                if (image != null) {
                    image = image.ScaleSimple(42, 42, Gdk.InterpType.Bilinear);
                    notify_notification_set_icon_from_pixbuf(notification, image.Handle);
                }

                notify_notification_show(notification, IntPtr.Zero);
                g_object_unref(notification);
                notify_uninit();
            } catch (Exception e) {
                LogCore.Instance.PushError(Catalog.GetString("Cannot show notification"), e.Message, false);
            }
        }

        [DllImport("notify")]
        private static extern bool notify_init(string app_name);

        [DllImport("notify")]
        private static extern IntPtr notify_notification_new(string summary, string message, string icon, IntPtr widget);

        [DllImport("notify")]
        private static extern void notify_notification_set_timeout(IntPtr notification, int timeout);

        [DllImport("notify")] 
        private static extern void notify_notification_set_icon_from_pixbuf(IntPtr notification, IntPtr icon);

        [DllImport("notify")] 
        private static extern bool notify_notification_show(IntPtr notification, IntPtr error);

        [DllImport("libgobject-2.0-0.dll")]
        private static extern void g_object_unref(IntPtr o);

        [DllImport("notify")]
        private static extern void notify_uninit();
    }
}
