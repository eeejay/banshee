//
// SourceActions.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Banshee.Base;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Playlist;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class SourceActions : ActionGroup
    {
        private InterfaceActionService action_service;
        IHasSourceView source_view;
        
        public SourceActions (InterfaceActionService actionService, IHasSourceView sourceView) : base ("Source")
        {
            Add (new ActionEntry [] {
                new ActionEntry("NewPlaylistAction", Stock.New,
                    Catalog.GetString("_New Playlist"), "<control>N",
                    Catalog.GetString("Create a new empty playlist"), OnNewPlaylist),

                new ActionEntry("ImportSourceAction", null,
                    Catalog.GetString("Import Source"), null,
                    Catalog.GetString("Import source to library"), OnImportSourceAction),

                new ActionEntry("RenameSourceAction", "gtk-edit", 
                    "Rename", "F2", "Rename", OnRenameSourceAction),

                new ActionEntry("UnmapSourceAction", Stock.Delete,
                    "Unmap", "<shift>Delete", null, OnUnmapSourceAction),
                    
                new ActionEntry("SelectedSourcePropertiesAction", Stock.Properties,
                    "Source Properties", null, null, OnSelectedSourcePropertiesAction),
            });
                
            //ServiceManager.SourceManager.SourceUpdated += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceViewChanged += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceAdded += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceRemoved += OnPlayerEngineStateChanged;
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
            action_service = actionService;
            source_view = sourceView;
        }
            
#region Source Manager Handlers

        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            //this ["RenameSourceAction"].Sensitive = true;
            //action_service
        }

#endregion

#region Action Handlers

        private void OnNewPlaylist (object o, EventArgs args)
        {
            PlaylistSource playlist = new PlaylistSource ("New Playlist");
            ServiceManager.SourceManager.DefaultSource.AddChildSource (playlist);

            // TODO should begin editing the name after making it, but this changed
            // the ActiveSource to the new playlist and we don't want that.
            //source_view.BeginRenameSource (playlist);
        }
            
        private void OnImportSourceAction (object o, EventArgs args)
        {
        }

        private void OnRenameSourceAction (object o, EventArgs args)
        {
            source_view.BeginRenameSource (source_view.HighlightedSource);
        }

        private void OnUnmapSourceAction (object o, EventArgs args)
        {
        }

        private void OnSelectedSourcePropertiesAction (object o, EventArgs args)
        {
        }

#endregion
        
    }
}
