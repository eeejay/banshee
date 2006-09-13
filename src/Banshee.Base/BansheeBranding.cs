/***************************************************************************
 *  BansheeBranding.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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

namespace Banshee.Base
{
    public class BansheeBranding : ICustomBranding
    {
        private Gdk.Pixbuf about_box_logo;
        private Banshee.Gui.Dialogs.SplashScreen splash;
        
        public bool Initialize()
        {
            about_box_logo = Gdk.Pixbuf.LoadFromResource("banshee-logo.png");
            
            if(Globals.ArgumentQueue.Contains("hide")) {
                return true;
            }
            
            Globals.StartupInitializer.RunFinished += delegate {
                GLib.Timeout.Add(500, HideSplashScreen);
            };
            
            splash = new Banshee.Gui.Dialogs.SplashScreen(ApplicationLongName, 
                Gdk.Pixbuf.LoadFromResource("splash.png"));
            splash.Run();
            
            return true;
        }
        
        private bool HideSplashScreen()
        {
            if(splash != null) {
                splash.Hide();
                splash.Dispose();
                splash = null;
            }
            
            return false;
        }
        
        public string ApplicationLongName {
            get { return Catalog.GetString("Banshee Music Player"); }
        }
        
        public string ApplicationName {
            get { return Catalog.GetString("Banshee"); }
        }
        
        public string ApplicationIconName {
            get { return "music-player-banshee"; }
        }
        
        public Gdk.Pixbuf ApplicationLogo {
            get { return about_box_logo; }
        }
        
        public Gdk.Pixbuf AboutBoxLogo {
            get { return about_box_logo; }
        }
    }
}
