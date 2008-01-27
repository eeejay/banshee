/***************************************************************************
 *  TrackViewColumnWindow.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
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
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Banshee.TrackView.Columns;

namespace Banshee
{
    public class TrackViewColumnWindow : Gtk.Window
    {
        private class ColumnCheckButton : Gtk.CheckButton
        {
            private TrackViewColumn column;
            
            public ColumnCheckButton(TrackViewColumn column) : base(column.Title)
            {
                this.column = column;
                Active = column.VisibilityPreference;
            }
            
            protected override void OnToggled()
            {
                base.OnToggled();
                column.VisibilityPreference = Active;
            }
        }
    
        public TrackViewColumnWindow(List<TrackViewColumn> columns) 
            : base(Catalog.GetString("Choose Columns"))
        {
            BorderWidth = 10;
            SetPosition(WindowPosition.Center);
            TypeHint = Gdk.WindowTypeHint.Utility;
            Resizable = false;
            
            VBox vbox = new VBox();
            vbox.Spacing = 10;
            vbox.Show();
            
            Add(vbox);
            
            Label label = new Label();
            label.Markup = String.Format("<b>{0}</b>", 
                GLib.Markup.EscapeText(Catalog.GetString("Visible Playlist Columns")));
            label.Show();
            vbox.Add(label);
            
            Table table = new Table((uint)System.Math.Ceiling((double)columns.Count), 2, false);
            table.Show();
            table.ColumnSpacing = 15;
            table.RowSpacing = 5;
            vbox.Add(table);

            columns.Sort(new TrackViewColumn.IDComparer());

            int i = 0;
            foreach(TrackViewColumn column in columns) {
                ColumnCheckButton check_button = new ColumnCheckButton(column);
                check_button.Show();
                table.Attach(check_button, 
                    (uint)(i % 2), 
                    (uint)((i % 2) + 1), 
                    (uint)(i / 2), 
                    (uint)(i / 2) + 1,
                    AttachOptions.Fill,
                    AttachOptions.Fill,
                    0, 0);
                i++;
            }
            
            HButtonBox actionArea = new HButtonBox();
            actionArea.Show();
            actionArea.Layout = ButtonBoxStyle.End;    
            
            Button closeButton = new Button("gtk-close");
            closeButton.Clicked += OnCloseButtonClicked;
            closeButton.Show();
            actionArea.PackStart(closeButton);

            vbox.Add(actionArea);
        }
        
        private void OnCloseButtonClicked(object o, EventArgs args)
        {
            Destroy();
        }
    }
}
