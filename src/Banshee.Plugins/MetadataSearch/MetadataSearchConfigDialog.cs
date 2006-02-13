
/***************************************************************************
 *  MetadataSearchConfigDialog.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
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
using Gtk;
using GConf;
using Mono.Unix;

using Banshee.Base;
using Banshee.Widgets;

namespace Banshee.Plugins.MetadataSearch 
{
    public class MetadataSearchConfigPage : VBox
    {
        private MetadataSearchPlugin plugin;
        private Alignment warning_align;
        private RadioButton fetch_covers_only;
        private RadioButton fill_blank_info;
        private RadioButton overwrite_info;
        private Button rescan_button;
        
        public MetadataSearchConfigPage(MetadataSearchPlugin plugin) : base()
        {
            this.plugin = plugin;
            BuildWidget();
        }
        
        private void BuildWidget()
        {
            Spacing = 10;
            
            Label title = new Label();
            title.Markup = String.Format("<big><b>{0}</b></big>", 
                                         GLib.Markup.EscapeText(Catalog.GetString("Metadata and Cover Art Searching")));
            title.Xalign = 0.0f;
            
            PackStart(title, false, false, 0);

            VBox box = new VBox();
            box.Spacing = 5;
            
            fetch_covers_only = new RadioButton(Catalog.GetString(
                "Only download album cover artwork"));
            fill_blank_info = new RadioButton(fetch_covers_only, Catalog.GetString(
                "Download album cover artwork and fill in missing track data"));
            overwrite_info = new RadioButton(fetch_covers_only, Catalog.GetString(
                "Download album cover artwork and overwrite any existing track data"));

            rescan_button = new Button (Catalog.GetString ("Rescan Library"));
            rescan_button.Sensitive = !plugin.IsScanning;
            rescan_button.Clicked += OnRescan;
            HBox rescan_box = new HBox ();
            rescan_box.PackStart(rescan_button, false, false, 0);
            rescan_box.PackStart(new Label(""), true, true, 0);
            
            fetch_covers_only.Toggled += OnToggled;
            fill_blank_info.Toggled += OnToggled;
            overwrite_info.Toggled += OnToggled;
            
            warning_align = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            warning_align.LeftPadding = 25;
            warning_align.TopPadding = 10;
            HBox warning_box = new HBox();
            warning_box.Spacing = 10;
            Image warning_image = new Image(Stock.DialogWarning, IconSize.Menu);
            warning_image.Yalign = 0.0f;
            
            Label warning_label = new Label();
            warning_label.Markup = "<small><b>" + GLib.Markup.EscapeText(Catalog.GetString("Warning")) + ":</b> " + 
                GLib.Markup.EscapeText(Catalog.GetString(
                    "This option can usually correct minor mistakes in metadata, however on rare occasions " + 
                    "metadata may be incorrectly updated from MusicBrainz.")) + "</small>";
            warning_label.Wrap = true;
            warning_label.Selectable = true;
            
            warning_box.PackStart(warning_image, false, false, 0);
            warning_box.PackStart(warning_label, false, false, 0);
            warning_align.Add(warning_box);
            
            box.PackStart(fetch_covers_only, false, false, 0);
            box.PackStart(fill_blank_info, false, false, 0);
            box.PackStart(overwrite_info, false, false, 0);
            box.PackStart(warning_align, false, false, 0);
            box.PackStart(rescan_box, false, false, 0);
            
            PackStart(box, false, false, 0);
            
            ShowAll();
            
            warning_align.Hide();
            
            switch(plugin.FetchMethod) {
            case FetchMethod.FillBlank:
                fill_blank_info.Active = true;
                break;
            case FetchMethod.Overwrite:
                overwrite_info.Active = true;
                warning_align.Show();
                break;
            default:
                fetch_covers_only.Active = true;
                break;
            }

            plugin.ScanStarted += OnScanStarted;
            plugin.ScanEnded += OnScanEnded;
        }

        private void OnScanStarted(object o, EventArgs args)
        {
            Application.Invoke(delegate {
                rescan_button.Sensitive = false;
            });
        }

        private void OnScanEnded(object o, EventArgs args)
        {
            Application.Invoke(delegate {
                rescan_button.Sensitive = true;
            });
        }

        private void OnRescan(object o, EventArgs args)
        {
            plugin.RescanLibrary ();
            rescan_button.Sensitive = false;
        }
        
        private void OnToggled(object o, EventArgs args)
        {
            if(!(o as ToggleButton).Active) {
                return;
            }
            
            warning_align.Visible = o == overwrite_info;
            
            if(o == fill_blank_info) {
                plugin.FetchMethod = FetchMethod.FillBlank;
            } else if(o == overwrite_info) {
                plugin.FetchMethod = FetchMethod.Overwrite;
            } else {
                plugin.FetchMethod = FetchMethod.CoversOnly;
            }
        }
    }
}
