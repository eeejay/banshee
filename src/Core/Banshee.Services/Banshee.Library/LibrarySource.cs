//
// LibrarySource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Collections;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Library
{
    public class LibrarySource : PrimarySource
    {
        public LibrarySource () : base (Catalog.GetString ("Library"), Catalog.GetString ("Library"), "Library", 1)
        {
            Properties.SetStringList ("Icon.Name", "go-home", "user-home", "source-library");
            Properties.SetString ("GtkActionPath", "/LibraryContextMenu");
            AfterInitialized ();

            Properties.SetString ("RemoveTracksActionLabel", Catalog.GetString ("Remove From Library"));
        }
        
        protected override void DeleteTrackRange (TrackListDatabaseModel model, RangeCollection.Range range)
        {
            for (int i = range.Start; i <= range.End; i++) {
                DatabaseTrackInfo track = model [i] as DatabaseTrackInfo;
                if (track == null)
                    continue;

                try {
                    // Remove from file system
                    try {
                        Banshee.IO.Utilities.DeleteFileTrimmingParentDirectories (track.Uri);
                    } catch (System.IO.FileNotFoundException) {
                    } catch (System.IO.DirectoryNotFoundException) {
                    }
                } catch (Exception e) {
                    ErrorSource.AddMessage (e.Message, track.Uri.ToString ());
                }

                // Remove from database
                RemoveTrackRange (model, range);
            }
        }

        public override bool AcceptsInputFromSource (Source source)
        {
            return source is IImportSource;
        }

        public override void MergeSourceInput (Source source, SourceMergeType mergeType)
        {
            if (!(source is IImportSource) || mergeType != SourceMergeType.Source) {
                return;
            }
            
            ((IImportSource)source).Import ();
        }
        
        public override bool CanRename {
            get { return false; }
        }
    }
}
