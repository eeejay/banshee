/***************************************************************************
 *  RadioSource.cs
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
using System.IO;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Widgets;
using Banshee.Sources;
using Banshee.MediaEngine;
using Banshee.Playlists.Formats.Xspf;
 
namespace Banshee.Plugins.Radio
{   
    public class RadioSource : Source
    {
        private static readonly Gdk.Pixbuf refresh_pixbuf = IconThemeUtils.LoadIcon(22, Stock.Refresh);
        private static readonly Gdk.Pixbuf error_pixbuf = IconThemeUtils.LoadIcon(22, Stock.DialogError);
        
        private RadioPlugin plugin;
        
        private StationView view;
        private StationModel model;
        
        private VBox box;
        private HighlightMessageArea status_bar;
        
        private RadioTrackInfo last_loaded_track;
        
        public override string ActionPath {
            get { return "/RadioSourcePopup"; }
        }
        
        public RadioSource(RadioPlugin plugin) : base(Catalog.GetString("Radio"), 150)
        {
            this.plugin = plugin;
            
            PlayerEngineCore.EventChanged += OnPlayerEventChanged;
            PlayerEngineCore.StateChanged += OnPlayerStateChanged;
            
            plugin.StationManager.StationsLoaded += delegate {
                if(status_bar != null) {
                    status_bar.Hide();
                }
                
                OnUpdated();
            };
            
            plugin.StationManager.StationsRefreshing += delegate {
                if(status_bar != null) {
                    status_bar.Message = String.Format("<big>{0}</big>", GLib.Markup.EscapeText(Catalog.GetString(
                        "Refreshing radio stations from the Banshee Radio Web Service")));
                    status_bar.Pixbuf = refresh_pixbuf;
                    status_bar.ShowCloseButton = false;
                    status_bar.Show();
                }
            };
            
            plugin.StationManager.StationsLoadFailed += delegate(object o, StationManager.StationsLoadFailedArgs args) {
                if(status_bar != null) {
                    status_bar.Message = String.Format("<big>{0}</big>", GLib.Markup.EscapeText(Catalog.GetString(
                        "Failed to load radio stations: " + args.Message)));
                    status_bar.Pixbuf = error_pixbuf;
                    status_bar.ShowCloseButton = true;
                    status_bar.Show();
                }
            };
            
            plugin.PopupActions.GetAction("CopyUriAction").Activated += OnCopyUri;
            plugin.PopupActions.GetAction("NewStationGroupAction").Activated += OnNewStationGroup;
            plugin.PopupActions.GetAction("RemoveAction").Activated += OnRemoveStation;
            plugin.PopupActions.GetAction("EditAction").Activated += OnEditStation;
            
            BuildInterface();
        }
        
        private void BuildInterface()
        {
            box = new VBox();
            
            model = new StationModel(plugin);
            view = new StationView(model);
            view.RowActivated += OnViewRowActivated;
            view.Popup += OnViewPopup;
            view.Selection.Changed += OnViewSelectionChanged;
            
            ScrolledWindow view_scroll = new ScrolledWindow();
            view_scroll.HscrollbarPolicy = PolicyType.Never;
            view_scroll.VscrollbarPolicy = PolicyType.Automatic;
            view_scroll.ShadowType = ShadowType.In;
            
            view_scroll.Add(view);
            
            status_bar = new HighlightMessageArea();
            status_bar.BorderWidth = 5;
            status_bar.LeftPadding = 15;
            
            box.PackStart(view_scroll, true, true, 0);
            box.PackStart(status_bar, false, false, 0);
            
            view_scroll.ShowAll();
            box.Show();
            status_bar.Hide();
        }
        
        private void OnPlayerStateChanged(object o, PlayerEngineStateArgs args)
        {
            view.QueueDraw();
            
            if(args.State == PlayerEngineState.Loaded && PlayerEngineCore.CurrentTrack is RadioTrackInfo) {
                last_loaded_track = PlayerEngineCore.CurrentTrack as RadioTrackInfo;
            }
        }
        
        private void OnPlayerEventChanged(object o, PlayerEngineEventArgs args)
        {
            if(args.Event == PlayerEngineEvent.Error && last_loaded_track != null) {
                last_loaded_track.PlayNextStream();
            }
        }
        
        private void OnViewSelectionChanged(object o, EventArgs args)
        {
            bool can_edit = false;
            
            if(view.SelectedStationGroup != null) {
                can_edit = view.SelectedStationGroup.CanEdit;
            }
        
            plugin.PopupActions.GetAction("NewStationGroupAction").Visible = true;
            plugin.PopupActions.GetAction("CopyUriAction").Sensitive = view.SelectedTrack != null;
            plugin.PopupActions.GetAction("EditAction").Visible = can_edit;
            plugin.PopupActions.GetAction("RemoveAction").Visible = can_edit;
            plugin.PopupActions.GetAction("AddAction").Visible = can_edit && view.SelectedTrack == null;
        }
        
        private bool ShouldShowPopup()
        {
            foreach(Action action in plugin.PopupActions.ListActions()) {
                if(action.Visible) {
                    return true;
                }
            }
            
            return false;
        }
        
        private void OnViewPopup(object o, StationViewPopupArgs args) 
        {
            if(!ShouldShowPopup()) {
                return;
            }
            
            Menu menu = Globals.ActionManager.GetWidget("/StationViewPopup") as Menu;
            menu.ShowAll();
            menu.Popup(null, null, null, 0, args.Time);
        }
        
        private void OnViewRowActivated(object o, RowActivatedArgs args)
        {
            RadioTrackInfo radio_track = model.GetRadioTrackInfo(args.Path);
            if(radio_track != null) {
                radio_track.Play();
                return;
            }
            
            Track track = model.GetTrack(args.Path);
            if(track == null) {
                return;
            }
            
            radio_track = new RadioTrackInfo(track);
            radio_track.ParsingPlaylistEvent += OnTrackParsingPlaylistEvent;
            model.SetRadioTrackInfo(args.Path, radio_track);
            radio_track.Play();
        }
        
        private void OnTrackParsingPlaylistEvent(object o, EventArgs args)
        {
            view.QueueDraw();
        }
        
        private void EditStationGroup(StationGroup group)
        {
            string previous_path = group == null ? null : group.LocalPath;
            StationGroup new_group = group;
            
            StationGroupEditor editor = new StationGroupEditor(new_group);
            editor.Show();
            
            editor.Response += delegate(object eo, ResponseArgs eargs) {
                if(eargs.ResponseId != ResponseType.Ok) {
                    editor.Destroy();
                    return;
                }
                
                if(group != null) {
                    group.Title = editor.Value;
                    group.UpdatePath();
                }
                
                if(previous_path != null && new_group.LocalPath == previous_path) {
                    editor.Destroy();
                    return;
                }   
                
                if(new_group == null) {
                    new_group = new StationGroup(editor.Value);
                }
                
                if(File.Exists(new_group.LocalPath)) {
                    editor.ShowExistsMessage();
                    editor.FocusEntry();
                    return;
                }
                
                if(group == null) {
                    plugin.StationManager.LoadStationGroup(new_group);
                } else {
                    model.UpdateStationGroup(group);
                    try {
                        File.Move(previous_path, group.LocalPath);
                    } catch {
                    }
                }
                
                editor.Destroy();
            };
        }
        
        private void OnNewStationGroup(object o, EventArgs args)
        {
            EditStationGroup(null);
        }
        
        private void OnRemoveStation(object o, EventArgs args)
        {
            if(!view.SelectedStationGroup.CanEdit) {
                return;
            }
            
            if(view.SelectedTrack == null) {
                plugin.StationManager.RemoveStationGroup(view.SelectedStationGroup);
            }
        }
        
        private void OnEditStation(object o, EventArgs args)
        {
            if(!view.SelectedStationGroup.CanEdit) {
                return;
            }
            
            if(view.SelectedTrack == null) {
                EditStationGroup(view.SelectedStationGroup);
            }
        }
        
        private void OnCopyUri(object o, EventArgs args)
        {
            Track track = view.SelectedTrack;
            RadioTrackInfo radio_track = view.SelectedRadioTrackInfo;
            
            string uri = null;
            
            if(radio_track != null) {
                uri = radio_track.Uri.AbsoluteUri;
            } else if(track != null && track.Locations.Count > 0) {
                uri = track.Locations[0].AbsoluteUri;
            } else {
                return;
            }
            
            Clipboard clipboard = Clipboard.Get(Gdk.Selection.Clipboard);
            clipboard.Text = uri;
        }
        
        public override Gtk.Widget ViewWidget {
            get { return box; }
        }
        
        public override int Count {
            get { return plugin.StationManager.TotalStations; }
        }
        
        public override bool SearchEnabled {
            get { return false; }
        }
        
        private static Gdk.Pixbuf icon = Gdk.Pixbuf.LoadFromResource("source-radio.png");
        
        public override Gdk.Pixbuf Icon {
            get { return icon; } 
        }
    }
}
