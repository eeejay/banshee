/***************************************************************************
 *  FileImportSource.cs
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

namespace Banshee.Base
{
    public class FileImportSource : IImportSource
    {
        private static FileImportSource instance;
        public static FileImportSource Instance {
            get {
                if(instance == null) {
                    instance = new FileImportSource();
                }
                
                return instance;
            }
        }
        
        private FileImportSource()
        {
        }
    
        public void Import()
        {
            Banshee.Gui.Dialogs.FileChooserDialog chooser = new Banshee.Gui.Dialogs.FileChooserDialog(
                Catalog.GetString("Import Files to Library"),
                FileChooserAction.Open
            );
            
            chooser.AddButton(Stock.Cancel, ResponseType.Cancel);
            chooser.AddButton(Stock.Open, ResponseType.Ok);
            
            chooser.SelectMultiple = true;
            chooser.DefaultResponse = ResponseType.Ok;
            
            if(chooser.Run() == (int)ResponseType.Ok) {
                Banshee.Library.Import.QueueSource(chooser.Uris);
            }
            
            chooser.Destroy();
        }
        
        public string Name {
            get { return Catalog.GetString("Local Files"); }
        }
        
        private Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, Stock.Open);
        public Gdk.Pixbuf Icon {
            get { return icon; }
        }
    }
}
