//
// MoblinService.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Novell, Inc.
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

using Hyena;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Gui;

using Mutter;

namespace Banshee.Moblin
{
    public class MoblinService : IExtensionService
    {
        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
        
        public MoblinService ()
        {
        }
        
        void IExtensionService.Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();
        
            if (!ServiceStartup ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }
        
        private void OnServiceStarted (ServiceStartedArgs args) 
        {
            if (args.Service is Banshee.Gui.InterfaceActionService) {
                interface_action_service = (InterfaceActionService)args.Service;
            } else if (args.Service is GtkElementsService) {
                elements_service = (GtkElementsService)args.Service;
            }
                    
            ServiceStartup ();
        }
        
        private bool ServiceStartup ()
        {
            if (elements_service == null || interface_action_service == null) {
                return false;
            }
            
            Initialize ();
            
            ServiceManager.ServiceStarted -= OnServiceStarted;
            
            return true;
        }
        
        private void Initialize ()
        {
            if (MoblinPanel.Instance != null) {
                var container = MoblinPanel.Instance.ParentContainer;
                foreach (var child in container.Children) {
                    container.Remove (child);
                }
                container.Add (new MediaPanelContents ());
                container.ShowAll ();
                
                if (MoblinPanel.Instance.ToolbarPanel != null) {
                    container.SetSizeRequest (
                        (int)MoblinPanel.Instance.ToolbarPanelWidth,
                        (int)MoblinPanel.Instance.ToolbarPanelHeight);
                }
            }
        }
        
        public void Dispose ()
        {
            if (MoblinPanel.Instance != null) {
                MoblinPanel.Instance.Dispose ();
            }
            
            interface_action_service = null;
            elements_service = null;
        }

        string IService.ServiceName {
            get { return "MoblinService"; }
        }
    }
}
