//
// MoveOnInfoSaveJob.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using System.IO;

using Mono.Unix;

using Banshee.Base;
using Banshee.Collection.Database;
using Banshee.Configuration.Schema;

namespace Banshee.Collection
{
    public class MoveOnInfoSaveJob : Banshee.Kernel.IInstanceCriticalJob
    {
        private DatabaseTrackInfo track;

        public string Name {
            get { return String.Format (Catalog.GetString ("Renaming {0}"), track.TrackTitle); }
        }

        public MoveOnInfoSaveJob (DatabaseTrackInfo track)
        {
            this.track = track;
        }

        public void Run ()
        {
            if (track == null) {
                return;
            }

            if (!LibrarySchema.MoveOnInfoSave.Get ()) {
                Hyena.Log.Debug ("Skipping scheduled rename, preference disabled after scheduling");
                return;
            }

            SafeUri old_uri = track.Uri;
            bool in_library = old_uri.AbsolutePath.StartsWith (track.PrimarySource.BaseDirectoryWithSeparator);

            if (!in_library) {
                return;
            }

            string new_filename = FileNamePattern.BuildFull (track.PrimarySource.BaseDirectory, track, Path.GetExtension (old_uri.ToString ()));
            SafeUri new_uri = new SafeUri (new_filename);

            if (!new_uri.Equals (old_uri) && !Banshee.IO.File.Exists (new_uri)) {
                Banshee.IO.File.Move (old_uri, new_uri);
                Banshee.IO.Utilities.TrimEmptyDirectories (old_uri);
                track.Uri = new_uri;
                track.Save ();
            }
        }
    }
}
