/***************************************************************************
 *  DriveComboBox.cs
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
using Gtk;

using Banshee.Cdrom;

namespace Banshee.Cdrom.Gui
{
    public class DriveComboBox : ComboBox
    {
        private IDriveFactory factory;
        private ListStore list;
        private bool recorders_only;
        
        public DriveComboBox(IDriveFactory factory) : this(factory, true)
        {
        }
        
        public DriveComboBox(IDriveFactory factory, bool recordersOnly)
        {
            this.factory = factory;
            this.recorders_only = recordersOnly;
            
            Model = list = new ListStore(typeof(string), typeof(IDrive));
            CellRenderer name_renderer = new CellRendererText();
            PackStart(name_renderer, true);
            AddAttribute(name_renderer, "text", 0);
            
            foreach(IDrive drive in factory) {
                AddDrive(drive);
            }
            
            factory.DriveAdded += delegate(object o, DriveArgs args) {
                AddDrive(args.Drive);
            };
            
            factory.DriveRemoved += delegate(object o, DriveArgs args) {
                RemoveDrive(args.Drive);
            };
        }
 
        private void AddDrive(IDrive drive)
        {
            if(recorders_only && drive is IRecorder) {
                list.AppendValues(drive.Name, drive);
            }
        }
        
        private void RemoveDrive(IDrive drive)
        {
            TreeIter iter;
            if(FindIterForDrive(drive, out iter)) {
                list.Remove(ref iter);
                Active = 0;
            }
        }
        
        private bool FindIterForDrive(IDrive drive, out TreeIter iter)
        {
            iter = TreeIter.Zero;
            
            for(int i = 0, n = list.IterNChildren(); i < n; i++) {
                TreeIter tmp_iter;
                if(list.IterNthChild(out tmp_iter, i)) {
                    IDrive tmp_drive = list.GetValue(tmp_iter, 1) as IDrive;
                    if(tmp_drive == drive) {
                        iter = tmp_iter;
                        return true;
                    }
                }
            }
            
            return false;
        }
 
        public IDrive SelectedDrive {
            get {
                TreeIter iter;
                if(GetActiveIter(out iter)) {
                    return list.GetValue(iter, 1) as IDrive;
                }
                
                return null;
            }
            
            set {
                TreeIter iter;
                if(FindIterForDrive(value, out iter)) {
                    SetActiveIter(iter);
                }
            }   
        }
        
        public IDriveFactory Factory {
            get { return factory; }
        }
    }
}
