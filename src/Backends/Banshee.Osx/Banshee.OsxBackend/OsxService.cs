//
// OsxService.cs
//
// Author:
//   Eoin Hennessy <eoin@randomrules.org>
//
// Copyright (C) 2008 Eoin Hennessy
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
using System.Collections;
using Gtk;
using Mono.Unix;

using Banshee.ServiceStack;
using Banshee.Gui;

using IgeMacIntegration;

namespace Banshee.OsxBackend
{
    public class OsxService : IExtensionService, IDisposable
    {
        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
        private uint ui_manager_id;
        private bool disposed;
        
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


        void Initialize ()
        {
            // add close action
            interface_action_service.GlobalActions.Add (new ActionEntry [] {
                new ActionEntry ("CloseAction", Stock.Close,
                    Catalog.GetString ("_Close"), "<Control>W",
                    Catalog.GetString ("Close"), CloseWindow)
            });

            // merge close menu item
            ui_manager_id = interface_action_service.UIManager.AddUiFromResource ("osx-ui-actions-layout.xml");      
            RegisterCloseHandler ();

            elements_service.PrimaryWindow.WindowStateEvent += WindowStateHandler;
            
            // bind gtk menu to globel osx menu 
            BindMenuBar ();

            // make menu more osx-like
            AdjustMainMenu ();

            // add dock handlers
            IgeMacDock doc = new IgeMacDock();
            doc.Clicked += OnDockClicked;
            doc.QuitActivate += OnDockQuitActivated;
        }
        
        public void Dispose ()
        {
            if (disposed) {
                return;
            }

            elements_service.PrimaryWindowClose = null;
            
            interface_action_service.GlobalActions.Remove ("CloseAction");
            interface_action_service.UIManager.RemoveUi (ui_manager_id);
        
            disposed = true;
        }
        
        string IService.ServiceName {
            get { return "OsxService"; }
        }

        private void OnDockClicked (object o, System.EventArgs args) 
        {
            SetWindowVisibility (true);
        }

        private void OnDockQuitActivated (object o, System.EventArgs args) 
        {
            Banshee.ServiceStack.Application.Shutdown ();
        }
        
        private void BindMenuBar ()
        {
            UIManager ui = interface_action_service.UIManager;

            // retreive and hide the gtk menu
            MenuShell menu = (MenuShell) ui.GetWidget ("/MainMenu");
            menu.Hide ();
            
            // bind menu 
            IgeMacMenu.MenuBar = menu;
        }

        private void AdjustMainMenu () {
            UIManager ui = interface_action_service.UIManager;

            MenuItem about_item = ui.GetWidget ("/MainMenu/HelpMenu/About") as MenuItem;
            MenuItem prefs_item = ui.GetWidget ("/MainMenu/EditMenu/Preferences") as MenuItem;
            MenuItem quit_item  = ui.GetWidget ("/MainMenu/MediaMenu/Quit") as MenuItem;

            IgeMacMenuGroup about_group = IgeMacMenu.AddAppMenuGroup ();
            IgeMacMenuGroup prefs_group = IgeMacMenu.AddAppMenuGroup ();

            IgeMacMenu.QuitMenuItem = quit_item;
            
            about_group.AddMenuItem (about_item, null);
            prefs_group.AddMenuItem (prefs_item, null);
        }

        private void RegisterCloseHandler ()
        {
            if (elements_service.PrimaryWindowClose == null) {
                elements_service.PrimaryWindowClose = OnPrimaryWindowClose;
            }
        }
        
        private bool OnPrimaryWindowClose ()
        {
            CloseWindow (null, null);
            return true;
        }

        private void CloseWindow (object o, EventArgs args)
        {
            SetWindowVisibility(false);
        }

        private void SetCloseMenuItemSensitivity (bool sensitivity) {
            UIManager ui = interface_action_service.UIManager;
            MenuItem close_item = ui.GetWidget ("/MainMenu/MediaMenu/ClosePlaceholder/Close") as MenuItem;
            close_item.Sensitive = sensitivity;
        }

        private void SetWindowVisibility (bool visible)
        {
            SetCloseMenuItemSensitivity(visible);
            if (elements_service.PrimaryWindow.Visible != visible) {
                elements_service.PrimaryWindow.ToggleVisibility ();
            }
        }

        private void WindowStateHandler (object obj, WindowStateEventArgs args) {
            switch (args.Event.NewWindowState) {
            case Gdk.WindowState.Iconified:
                SetCloseMenuItemSensitivity(false);
                break;
            case (Gdk.WindowState) 0:
                SetCloseMenuItemSensitivity(true);
                break;
            }
        }
    }
}
