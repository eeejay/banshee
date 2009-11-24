//
// EmusicImport.cs
//
// Author:
//   Eitan Isaacson <eitan@monotonous.org>
//
// Copyright (C) 2009 Eitan Isaacson
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
using System.Data;
using System.IO;
using System.Xml;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.Library;
using Banshee.ServiceStack;
using Banshee.Playlist;
using Banshee.Emusic;

using Migo.DownloadCore;
using Migo.TaskCore;

namespace Banshee.Emusic
{
    public sealed class EmusicImport : IImportSource
    {

        public EmusicImport ()
        {
        }

        public void Import ()
        {
            
            var chooser = Banshee.Gui.Dialogs.FileChooserDialog.CreateForImport (Catalog.GetString ("Import eMusic Downloads to Library"), true);
            Gtk.FileFilter ff = new Gtk.FileFilter();
            ff.Name = Catalog.GetString ("eMusic Files");
            ff.AddPattern("*.emx");
            chooser.AddFilter (ff);

            if (chooser.Run () == (int)Gtk.ResponseType.Ok)
                ServiceManager.Get<EmusicService> ().ImportEmx (chooser.Uris);
            
            chooser.Destroy ();
        }

        public bool CanImport {
            get { return true;}
        }

        public string Name {
            get { return Catalog.GetString ("eMusic Tracks"); }
        }

        public string ImportLabel {
            get { return Catalog.GetString ("C_hoose Files"); }
        }

        public string [] IconNames {
            get { return new string [] { "gtk-open" }; }
        }

        public int SortOrder {
            get { return 50; }
        }
    }
}
