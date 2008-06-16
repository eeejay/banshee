//
// InternetRadioSource.cs
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
using Mono.Unix;
using Gtk;

using Hyena;

using Banshee.Base;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Configuration;

using Banshee.Gui;

namespace Banshee.InternetRadio
{
    public class InternetRadioSource : PrimarySource, IDisposable
    {
        protected override string TypeUniqueId {
            get { return "internet-radio"; }
        }
        
        private uint ui_id;
        
        public InternetRadioSource () : base (Catalog.GetString ("Radio"), 
            Catalog.GetString ("Radio"), "internet-radio", 220)
        {
            Properties.SetString ("Icon.Name", "radio");
            IsLocal = false;
            
            AfterInitialized ();
            
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            uia_service.GlobalActions.AddImportant (
                new ActionEntry ("AddRadioStationAction", Stock.Add,
                    Catalog.GetString ("Add Station"), null,
                    Catalog.GetString ("Add a new Internet Radio station or playlist"),
                    OnAddStation)
            );
            
            ui_id = uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");
            
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.SetString ("GtkActionPath", "/InternetRadioContextMenu");
        }
        
        public override void Dispose ()
        {
            base.Dispose ();
            
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            if (ui_id > 0) {
                uia_service.UIManager.RemoveUi (ui_id);
                uia_service.GlobalActions.Remove ("AddRadioStationAction");
                ui_id = 0;    
            }
        }
        
        private void OnAddStation (object o, EventArgs args)
        {
            StationEditor editor = new StationEditor ();
            try {
                editor.Run ();
            } finally {
                editor.Destroy ();
            }
        }
               
        public override bool ShowBrowser {
            get { return false; }
        }
        
        public override bool CanRename {
            get { return false; }
        }
    }
}
