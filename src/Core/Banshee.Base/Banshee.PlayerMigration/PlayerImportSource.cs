/*
 *  Copyright (c) 2006 Sebastian Dröge <slomo@circular-chaos.org> 
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
using Mono.Unix;
using Banshee.Base;

namespace Banshee.PlayerMigration
{
    public class PlayerImportSource : IImportSource
    {
        private static PlayerImportSource instance = null;
        public static PlayerImportSource Instance {
            get {
                if (instance == null) {
                    instance = new PlayerImportSource ();
                }
                return instance;
            }
        }

        private PlayerImportSource () { }

        public void Import ()
        {
            try {
                PlayerImportDialog dialog = new PlayerImportDialog ();
                dialog.Run ();
                dialog.Destroy ();
            } catch (Exception) {}
        }

        public string Name
        {
                get { return Catalog.GetString("Alternate Media Players"); }
        }

        private Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, Stock.About);
        public Gdk.Pixbuf Icon
        {
                get { return icon; }
        }

    }
}
