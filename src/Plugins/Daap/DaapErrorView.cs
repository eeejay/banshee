/***************************************************************************
 *  DaapErrorView.cs
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

using Banshee.Base;
using Banshee.Widgets;

namespace Banshee.Plugins.Daap
{
    public enum DaapErrorType {
        BrokenAuthentication,
        InvalidAuthentication,
        UserDisconnect
    }
    
    public class DaapErrorView : ShadowContainer
    {
        private MessagePane pane;
        private bool info_link_clicked;
        private DaapSource source;
        private DaapErrorType failure;
        
        public DaapErrorView(DaapSource source, DaapErrorType failure) : base()
        {
            AppPaintable = true;
            BorderWidth = 10;
            
            this.source = source;
            this.failure = failure;
            
            pane = new MessagePane();
            pane.HeaderIcon = IconThemeUtils.LoadIcon(48, "face-surprise", Stock.DialogError);
            pane.ArrowIcon = IconThemeUtils.LoadIcon(24, "go-next", Stock.GoForward);
            pane.HeaderMarkup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText((failure == DaapErrorType.UserDisconnect 
                    ? Catalog.GetString("Disconnected from music share")
                    : Catalog.GetString("Unable to connect to music share"))));
                
            AddPaneItems();
            pane.Show();
            
            Add(pane);
        }
        
        private void AddPaneItems()
        {
            if(info_link_clicked) {
                LinkLabel link = new LinkLabel();
                link.Xalign = 0.0f;
                link.Markup = String.Format("<u>{0}</u>", GLib.Markup.EscapeText(Catalog.GetString(
                    "Back")));
                    
                link.Clicked += delegate(object o, EventArgs args) {
                    info_link_clicked = false;
                    pane.Clear();
                    AddPaneItems();
                };
                
                link.Show();
                pane.Append(link, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, 0, true, 
                    IconThemeUtils.LoadIcon(24, "go-previous", Stock.GoBack));
                
                pane.Append(Catalog.GetString(
                    "iTunes\u00ae 7 introduced new compatibility issues and currently only " +
                    "works with other iTunes\u00ae 7 clients.\n\n" + 
                    "No third-party clients can connect to iTunes\u00ae music shares anymore. " + 
                    "This is an intentional limitation by Apple in iTunes\u00ae 7 and we apologize for " + 
                    "the unfortunate inconvenience."
                ));
            } else {
                if(failure != DaapErrorType.UserDisconnect) {
                    Label header_label = new Label();
                    header_label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText(Catalog.GetString(
                        "Common reasons for connection failures:")));
                    header_label.Xalign = 0.0f;
                    header_label.Show();
                    
                    pane.Append(header_label, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, 0, false);
                    
                    pane.Append(Catalog.GetString("The provided login credentials are invalid"));
                    pane.Append(Catalog.GetString("The login process was canceled"));
                    pane.Append(Catalog.GetString("Too many users are connected to this share"));
                } else {
                    pane.Append(Catalog.GetString("You are no longer connected to this music share"));
                }
                
                if(failure == DaapErrorType.UserDisconnect || failure == DaapErrorType.InvalidAuthentication) {
                    Button button = new Button(Catalog.GetString("Try connecting again"));
                    button.Clicked += delegate { source.Activate(); };

                    HBox bbox = new HBox();
                    bbox.PackStart(button, false, false, 0);
                    bbox.ShowAll();

                    pane.Append(bbox, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, 0, false);
                    return;
                }
                
                LinkLabel link = new LinkLabel();
                link.Xalign = 0.0f;
                link.Markup = String.Format("<u>{0}</u>", GLib.Markup.EscapeText(Catalog.GetString(
                    "The music share is hosted by iTunes\u00ae 7")));
                    
                link.Clicked += delegate(object o, EventArgs args) {
                    info_link_clicked = true;
                    pane.Clear();
                    AddPaneItems();
                };
                
                link.Show();
                pane.Append(link, Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill, 0, true);
            }
        }
    }
}
