/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  UnsupportedDatabaseView.cs
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
using Gtk;
using IPod;

using Banshee.Base;
using Banshee.Widgets;

namespace Banshee.Dap.Ipod
{
    public class UnsupportedDatabaseView : Alignment
    {
        private MessagePane pane;
        private bool info_link_clicked = false;
        private IpodDap dap;
        
        public event EventHandler Refresh;
        
        public UnsupportedDatabaseView(IpodDap dap) : base(0.0f, 0.0f, 1.0f, 1.0f)
        {
            AppPaintable = true;
            BorderWidth = 10;
            
            this.dap = dap;
            
            pane = new MessagePane();
            pane.HeaderIcon = IconThemeUtils.LoadIcon(48, "face-surprise", Stock.DialogError);
            pane.ArrowIcon = IconThemeUtils.LoadIcon(24, "go-next", Stock.GoForward);
            pane.HeaderMarkup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText(Catalog.GetString("Unable to read your iPod")));
                
            AddPaneItems();
            pane.Show();
            
            Add(pane);
        }
        
        private void AddPaneItems()
        {
            if(info_link_clicked) {
                pane.Append(Catalog.GetString(GLib.Markup.EscapeText(
                    "You have used this iPod with a version of iTunes that saves a " +
                    "version of the song database for your iPod that is too new " +
                    "for Banshee to recognize.\n\n" +
                    
                    "Banshee can either rebuild the database or you will have to " +
                    "wait for the new iTunes version to be supported by Banshee."
                )));
            } else {
                LinkLabel link = new LinkLabel();
                link.Xalign = 0.0f;
                link.Markup = String.Format("<u>{0}</u>", GLib.Markup.EscapeText(Catalog.GetString(
                    "What is the reason for this?")));
                    
                link.Clicked += delegate(object o, EventArgs args) {
                    info_link_clicked = true;
                    pane.Clear();
                    AddPaneItems();
                };
                
                link.Show();
                pane.Append(link, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, 0, true);
            }
            
            LinkLabel rebuild_link = new LinkLabel();
            rebuild_link.Xalign = 0.0f;
            rebuild_link.Markup = String.Format("<u>{0}</u>", GLib.Markup.EscapeText(Catalog.GetString(
                "Rebuild iPod Database...")));
            rebuild_link.Clicked += OnRebuildDatabase; 
            rebuild_link.Show();
            pane.Append(rebuild_link, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, 0, true);
        }
        
        private void OnRebuildDatabase(object o, EventArgs args)
        {
            string title = Catalog.GetString("Confirm Rebuild iPod Database");
            HigMessageDialog md = new HigMessageDialog(null, 
                DialogFlags.DestroyWithParent, MessageType.Question,
                ButtonsType.Cancel,
                title,
                Catalog.GetString(
                    "Rebuilding your iPod database may take some time. Also note that " +
                    "any playlists you have on your iPod will be lost.\n\n" +
                    "Are you sure you want to rebuild your iPod database?"));
            md.Title = title;
            IconThemeUtils.SetWindowIcon(md);
            md.AddButton(Catalog.GetString("Rebuild Database"), Gtk.ResponseType.Yes, true);
            
            if(md.Run() != (int)ResponseType.Yes) {
                md.Destroy();
                return;
            }
            
            md.Destroy();

            pane.Clear();
            pane.HeaderIcon = dap.GetIcon(48);
            pane.HeaderMarkup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText(Catalog.GetString("Rebuilding iPod Database...")));
                
            DatabaseRebuilder rebuilder = new DatabaseRebuilder(dap);
            rebuilder.Finished += delegate(object o, EventArgs args) {
                OnRefresh();
            };
        }
        
        protected virtual void OnRefresh()
        {
            EventHandler handler = Refresh;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(!IsDrawable) {
                return false;
            }
            
            if(evnt.Window == GdkWindow) {
                GdkWindow.DrawRectangle(Style.BaseGC(State), true, 
                    Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
                Style.PaintShadow(Style, GdkWindow, State, ShadowType.In, Allocation, this, "frame", 
                    Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
            }
            
            if(Child != null) {
                PropagateExpose(Child, evnt);
            }
            
            return false;
        }
    }
}
