//
// RhythmboxPlayerImportSource.cs
//
// Author:
//   Sebastian Dröge <slomo@circular-chaos.org>
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2006 Sebastian Dröge
// Copyright (C) 2008 Paul Lange
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

using Mono.Unix;

using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.Library;
using Banshee.ServiceStack;
using Banshee.Playlist;

namespace Banshee.eMusic
{
    public sealed class eMusicImport : ThreadPoolImportSource
    {
        private string [] files_to_import;
        
        protected override bool ConfirmImport ()
        {
            var chooser = Banshee.Gui.Dialogs.FileChooserDialog.CreateForImport (Catalog.GetString ("Import eMusic Downloads to Library"), true);
            Gtk.FileFilter ff = new Gtk.FileFilter();
            ff.Name = Catalog.GetString ("eMusic Files");
            ff.AddPattern("*.emx");
            chooser.AddFilter (ff);

            int response = chooser.Run ();
            files_to_import = chooser.Uris;
            chooser.Destroy ();

            return response == (int)Gtk.ResponseType.Ok;
        }
        
        protected override void ImportCore ()
        {
            foreach (string uri in files_to_import)
            {
                using (var xml_reader = new XmlTextReader (uri))
                {
                    Console.WriteLine ("File: {0}", uri);
                    while (xml_reader.Read())
                    {
                        if (xml_reader.NodeType == XmlNodeType.Element &&
                            xml_reader.Name == "TRACKURL")
                        {
                            xml_reader.Read();
                            Console.WriteLine("URL: {0}", xml_reader.Value);
                        }
                    }
                  
                }
            }
        }

        public override bool CanImport {
            get { return true;}
        }

        public override string Name {
            get { return Catalog.GetString ("eMusic Album"); }
        }

        public override string [] IconNames {
            get { return new string [] { "gtk-open" }; }
        }

        public override int SortOrder {
            get { return 50; }
        }
    }
}
