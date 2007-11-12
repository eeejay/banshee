// 
// ImageFileChooserDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Mono.Unix;
using Gtk;

namespace Banshee.Gui.Dialogs
{
    public class ImageFileChooserDialog : Banshee.Gui.Dialogs.FileChooserDialog
    {
        private Image preview = new Image ();
    
        public ImageFileChooserDialog () : base (Catalog.GetString("Select album cover image"), FileChooserAction.Open)
        {
            AddButton (Stock.Cancel, ResponseType.Cancel);
            AddButton (Stock.Open, ResponseType.Ok);
            
            DefaultResponse = ResponseType.Ok;
            
            FileFilter filter = new FileFilter ();
            filter.Name = Catalog.GetString ("All image files");
            filter.AddMimeType ("image/jpeg");
            filter.AddMimeType ("image/png");
            AddFilter (filter);
            Filter = filter;
            
            filter = new FileFilter ();
            filter.Name = Catalog.GetString ("JPEG image files");
            filter.AddMimeType ("image/jpeg");
            AddFilter (filter);
            
            filter = new FileFilter ();
            filter.Name = Catalog.GetString ("PNG image files");
            filter.AddMimeType ("image/png");
            AddFilter (filter);
            
            PreviewWidget = preview;
        }
        
        protected override void OnUpdatePreview()
        {
            try {
                if (String.IsNullOrEmpty (PreviewFilename)) {
                    throw new ApplicationException ();
                }
                
                Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (PreviewFilename);
                preview.Pixbuf = pixbuf.ScaleSimple (100, 100, Gdk.InterpType.Bilinear);
                preview.Show ();
            } catch {
                preview.Hide ();
            }
        }
    }        
}
