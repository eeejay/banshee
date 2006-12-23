/***************************************************************************
 *  FileChooserDialog.cs
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

using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.Gui.Dialogs
{
    public class FileChooserDialog : Gtk.FileChooserDialog
    {
        public FileChooserDialog(string title, FileChooserAction action) : base(title, null, action)
        {
            SetCurrentFolderUri(LastFileChooserUri.Get(Environment.GetFolderPath(Environment.SpecialFolder.Personal)));
        }
        
        protected override void OnResponse(ResponseType response)
        {
            base.OnResponse(response);
            LastFileChooserUri.Set(CurrentFolderUri);
        }
        
        public static readonly SchemaEntry<string> LastFileChooserUri = new SchemaEntry<string>(
            "player_window", "last_file_chooser_uri",
            String.Empty,
            "URI",
            "URI of last file folder"
        );
    }
}
