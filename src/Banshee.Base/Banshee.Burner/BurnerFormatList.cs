/***************************************************************************
 *  BurnerFormatList.cs
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
using System.Collections.Generic;

using Mono.Unix;
using Gtk;

using Banshee.Cdrom;

namespace Banshee.Burner
{
    public class BurnerFormatList : Gtk.HBox
    {
        private class Format 
        {
            public Format(string name, string display)
            {
                Name = name;
                Display = display;
                Button = null;
            }
            
            public string Name;
            public string Display;
            public RadioButton Button;
        }
        
        private Dictionary<string, Format> format_table = new Dictionary<string, Format>();
        private string last_format = null;
        
        public event EventHandler FormatChanged;
        
        public BurnerFormatList()
        {
            format_table.Add("audio", new Format("audio", Catalog.GetString("Audio")));
            format_table.Add("mp3", new Format("mp3", Catalog.GetString("MP3")));
            format_table.Add("data", new Format("data", Catalog.GetString("Data")));
        
            RadioButton group = null;
            foreach(string key in new string [] { "audio", "mp3", "data" }) {
                Format format = format_table[key];
                
                if(group == null) {
                    format.Button = new RadioButton(format.Display);
                    group = format.Button;
                } else {
                    format.Button = new RadioButton(group, format.Display);
                }
                
                format.Button.Toggled += delegate {
                    string format = SelectedFormat;
                    if(format == last_format) {
                        return;
                    }
                    
                    last_format = format;
                    OnFormatChanged();
                };
                
                PackStart(format.Button, false, false, 0);
                format.Button.Show();
            }
        
            Spacing = 6;
        }
        
        protected virtual void OnFormatChanged()
        {
            EventHandler handler = FormatChanged;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        public string SelectedFormat {
            get { 
                foreach(Format format in format_table.Values) {
                    if(format.Button.Active) {
                        return format.Name;
                    }
                }
                
                return null;
            }
            
            set {
                if(value == null) {
                    return;
                }

                Format format = format_table[value];
                if(format == null) {
                    throw new ApplicationException("Invalid format '" + format.Name + "'");
                }
                
                format.Button.Active = true;
            }
        }
    }
}
