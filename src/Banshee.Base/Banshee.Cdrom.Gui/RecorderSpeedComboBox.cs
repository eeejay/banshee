/***************************************************************************
 *  RecorderSpeedComboBox.cs
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

using Banshee.Cdrom;

namespace Banshee.Cdrom.Gui
{
    public class RecorderSpeedComboBox : ComboBox
    {
        private IRecorder recorder;
        private ListStore list;
        
        public RecorderSpeedComboBox(IRecorder recorder)
        {
            this.recorder = recorder;
            
            Model = list = new ListStore(typeof(string), typeof(string), typeof(int));
            
            CellRenderer name_renderer = new CellRendererText();
            PackStart(name_renderer, true);
            AddAttribute(name_renderer, "text", 0);
            
            CellRenderer speed_renderer = new CellRendererText();
            PackStart(speed_renderer, false);
            AddAttribute(speed_renderer, "markup", 1);
            
            BuildModel();
        }
        
        private void BuildModel()
        {
            list.Clear();

            if(recorder == null) {
                list.AppendValues(Catalog.GetString("Unkown"), "", 0);
                return;
            }
                        
            string [] speed_labels = new string [] {
                Catalog.GetString("Maximum"),
                Catalog.GetString("High"),
                Catalog.GetString("Medium"),
                Catalog.GetString("Low")
            };
            
            if(recorder.MaxWriteSpeed < 8) {
                list.AppendValues(speed_labels[0], "<i>" + 
                    String.Format(Catalog.GetString("{0}x"), recorder.MaxWriteSpeed) + 
                    "</i> ", recorder.MaxWriteSpeed);
                Active = 0;
                return;
            }

            for(int i = 0, n = speed_labels.Length; i < n; i++) {
                int speed = (int)((double)recorder.MaxWriteSpeed * (double)((n - i) / (double)n));
                if(speed < recorder.MinWriteSpeed) {
                    speed = recorder.MinWriteSpeed;
                }
                
                list.AppendValues(speed_labels[i], "<i>" + 
                    String.Format(Catalog.GetString("{0}x"), speed) + 
                    "</i> ", speed);
            }
            
            Active = 0;
        }
        
        private bool FindIterForSpeed(int speed, out TreeIter iter)
        {
            iter = TreeIter.Zero;
            
            for(int i = 0, n = list.IterNChildren(); i < n; i++) {
                TreeIter tmp_iter;
                if(list.IterNthChild(out tmp_iter, i)) {
                    int tmp_speed = (int)list.GetValue(tmp_iter, 2);
                    if(tmp_speed == speed) {
                        iter = tmp_iter;
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        public IRecorder Recorder {
            get { return recorder; }
            set {
                recorder = value;
                BuildModel();
            }
        }
        
        public int Speed {
            get { 
                TreeIter iter;
                if(GetActiveIter(out iter)) {
                    return (int)list.GetValue(iter, 2);
                }
                
                return recorder.MaxWriteSpeed;
            }
            
            set {
                TreeIter iter;
                if(FindIterForSpeed(value, out iter)) {
                    SetActiveIter(iter);
                }
            }
        }
    }
}
