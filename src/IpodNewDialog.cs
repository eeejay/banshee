/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  IpodNewDialog.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using Mono.Unix;
using Gtk;
using Glade;
using IPod;

namespace Banshee
{
    public class IpodNewDialog
    {
        private Device device;
        private Glade.XML glade;
        
        public IpodNewDialog(Device device)
        {
            this.device = device;
            
            glade = new Glade.XML(null, "newipod.glade", "NewIpodWindow", null);
            glade.Autoconnect(this);
                
            (glade["NewIpodWindow"] as Window).Icon = Gdk.Pixbuf.LoadFromResource("ipod-48.png");
            (glade["ImageIpod"] as Image).Pixbuf = Gdk.Pixbuf.LoadFromResource("ipod-48.png");
            
            if(device.SerialNumber != null) {
                (glade["SerialNumberLabel"] as Label).Markup = "<small>" +
                    device.SerialNumber + "</small>";
                glade["SerialNumberLabel"].Visible = true;
            }
			
			glade["NewIpodWindow"].Show();
        }
        
        private void OnSaveButtonClicked(object o, EventArgs args)
        {
            if(device == null)
                return;
            
            device.Name = (glade["IpodNameEntry"] as Entry).Text;
            device.UserName = (glade["UserNameEntry"] as Entry).Text;
            device.Save();
            
            glade["NewIpodWindow"].Destroy();
        }
    }
}
