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
using Banshee.Preferences;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Library
{
    public abstract class LibrarySource : PrimarySource
    {
        // Deprecated, don't use in new code
        internal static readonly Banshee.Configuration.SchemaEntry<string> OldLocationSchema = new Banshee.Configuration.SchemaEntry<string> ("library", "base_location", null, null, null);

        private Banshee.Configuration.SchemaEntry<string> base_dir_schema;

        public LibrarySource (string label, string name, int order) : base (label, label, name, order)
        {
            Properties.SetString ("GtkActionPath", "/LibraryContextMenu");
            Properties.SetString ("RemoveTracksActionLabel", Catalog.GetString ("Remove From Library"));
            IsLocal = true;
            base_dir_schema = CreateSchema<string> ("library-location", null, "The base directory under which files for this library are stored", null);
            AfterInitialized ();

            Section library_section = PreferencesPage.Add (new Section ("library-location",
                // Translators: {0} is the library name, eg 'Music Library' or 'Podcasts'
                String.Format (Catalog.GetString ("{0} Folder"), Name), 2));

            library_section.Add (base_dir_schema);
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

        public override string PreferencesPageId {
            get { return UniqueId; }
        }

        public override string BaseDirectory {
            get {
                string dir = base_dir_schema.Get ();
                if (dir == null) {
                    BaseDirectory = dir = DefaultBaseDirectory;
                }
                return dir;
            }
            protected set {
                base_dir_schema.Set (value);
                base.BaseDirectory = value;
            }
        }

        public abstract string DefaultBaseDirectory { get; }

        public override bool Indexable {
            get { return true; }
        }

        /*public override void CopyTrackTo (DatabaseTrackInfo track, SafeUri uri, UserJob job)
        {
            Banshee.IO.File.Copy (track.Uri, uri, false);
        }*/

        protected override bool DeleteTrack (DatabaseTrackInfo track)
        {
            try {
                Banshee.IO.Utilities.DeleteFileTrimmingParentDirectories (track.Uri);
            } catch (System.IO.FileNotFoundException) {
            } catch (System.IO.DirectoryNotFoundException) {
            }

            return true;
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
                string path = FileNamePattern.BuildFull (BaseDirectory, track);
                SafeUri uri = new SafeUri (path);

                // Make sure it's not already in the library
                // TODO optimize - no need to recrate this int [] every time
                if (DatabaseTrackInfo.ContainsUri (uri, new int [] {DbId})) {
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
    }
}
