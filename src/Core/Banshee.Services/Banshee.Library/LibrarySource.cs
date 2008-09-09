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
        public LibrarySource (string label, string name, int order) : base (label, label, name, order)
        {
            Properties.SetString ("GtkActionPath", "/LibraryContextMenu");
            Properties.SetString ("RemoveTracksActionLabel", Catalog.GetString ("Remove From Library"));
            IsLocal = true;
            AfterInitialized ();
        }

        public string AttributesCondition {
            get { return String.Format ("((CoreTracks.Attributes & {0}) == {0} AND (CoreTracks.Attributes & {1}) == 0)", (int)MediaTypes, (int)NotMediaTypes); }
        }

        private string sync_condition;
        public string SyncCondition {
            get { return sync_condition; }
            protected set { sync_condition = value; }
        }

        private TrackMediaAttributes media_types;
        protected TrackMediaAttributes MediaTypes {
            get { return media_types; }
            set { media_types = value; }
        }

        private TrackMediaAttributes not_media_types = TrackMediaAttributes.None;
        protected TrackMediaAttributes NotMediaTypes {
            get { return not_media_types; }
            set { not_media_types = value; }
        }

        public override string BaseDirectory {
            get { return Paths.CachedLibraryLocation; }
        }
        
        public override bool Indexable {
            get { return true; }
        }

        /*public override void CopyTrackTo (DatabaseTrackInfo track, SafeUri uri, UserJob job)
        {
            Banshee.IO.File.Copy (track.Uri, uri, false);
        }*/

        protected override void DeleteTrack (DatabaseTrackInfo track)
        {
            try {
                Banshee.IO.Utilities.DeleteFileTrimmingParentDirectories (track.Uri);
            } catch (System.IO.FileNotFoundException) {
            } catch (System.IO.DirectoryNotFoundException) {
            }
        }

        protected override void AddTrack (DatabaseTrackInfo track)
        {
            // Ignore if already have it
            if (track.PrimarySourceId == DbId)
                return;

            PrimarySource source = track.PrimarySource;

            // If it's from a local primary source, change it's PrimarySource
            if (source.IsLocal || source is LibrarySource) {
                track.PrimarySource = this;

                if (!(source is LibrarySource)) {
                    track.CopyToLibraryIfAppropriate (false);
                }

                track.Save (false);
                source.NotifyTracksChanged ();
            } else {
                // Figure out where we should put it if were to copy it
                string path = FileNamePattern.BuildFull (track);
                SafeUri uri = new SafeUri (path);

                // Make sure it's not already in the library
                if (DatabaseTrackInfo.ContainsUri (uri, Paths.MakePathRelative (uri.AbsolutePath, BaseDirectory) ?? uri.AbsoluteUri, new int [] {DbId})) {
                    return;
                }

                // Since it's not, copy it and create a new TrackInfo object
                track.PrimarySource.CopyTrackTo (track, uri, AddTrackJob);

                // Create a new entry in CoreTracks for the copied file
                DatabaseTrackInfo new_track = new DatabaseTrackInfo (track);
                new_track.Uri = uri;
                new_track.PrimarySource = this;
                new_track.Save (false);
            }
        }
        
        public static int GetTrackIdForUri (string uri)
        {
            return DatabaseTrackInfo.GetTrackIdForUri (new SafeUri (uri), Paths.MakePathRelative (uri, Paths.LibraryLocation), LibraryIds);
        }
        
        private static int [] library_ids;
        private static int [] LibraryIds {
            get {
                return library_ids ?? library_ids = new int [] {ServiceManager.SourceManager.MusicLibrary.DbId, ServiceManager.SourceManager.VideoLibrary.DbId};
            }
        }
    }
}
