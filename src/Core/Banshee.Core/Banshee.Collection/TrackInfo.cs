
// TrackInfo.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Reflection;
using System.Collections.Generic;
using Mono.Unix;

using Hyena;
using Hyena.Data;
using Banshee.Base;
using Banshee.Streaming;

namespace Banshee.Collection
{
    public class TrackInfo : CacheableItem, ITrackInfo
    {
        public class ExportableAttribute : Attribute
        {
            private string export_name;
            public string ExportName {
                get { return export_name; }
                set { export_name = value; }
            }
        }
    
        public delegate bool IsPlayingHandler (TrackInfo track);
        public static IsPlayingHandler IsPlayingMethod;
            
        private SafeUri uri;
        private SafeUri more_info_uri;
        private string mimetype;
        private long filesize;

        private string artist_name;
        private string album_title;
        private string album_artist;
        private bool is_compilation;
        private string track_title;
        private string genre;
        private string composer;
        private string conductor;
        private string grouping;
        private string copyright;
        private string license_uri;
        private string musicbrainz_id;

        private string comment;
        private int track_number;
        private int track_count;
        private int disc_number;
        private int disc_count;
        private int year;
        private int rating;
        private int bpm;
        private int bit_rate;

        private TimeSpan duration;
        private DateTime release_date;
        private DateTime date_added;

        private int play_count;
        private int skip_count;
        private DateTime last_played;
        private DateTime last_skipped;
        
        private StreamPlaybackError playback_error = StreamPlaybackError.None;

        public TrackInfo ()
        {
        }

        public virtual void IncrementPlayCount ()
        {
            LastPlayed = DateTime.Now;
            PlayCount++;
        }

        public virtual void IncrementSkipCount ()
        {
            LastSkipped = DateTime.Now;
            SkipCount++;
        }

        public override string ToString ()
        {
            return String.Format ("{0} - {1} (on {2}) <{3}> [{4}]", ArtistName, TrackTitle, 
                AlbumTitle, Duration, Uri == null ? "<unknown>" : Uri.AbsoluteUri);
        }

        public virtual bool TrackEqual (TrackInfo track)
        {
            if (track == null || track.Uri == null || Uri == null) {
                return false;
            }
            
            return track.Uri.AbsoluteUri == Uri.AbsoluteUri;
        }
        
        public bool ArtistAlbumEqual (TrackInfo track)
        {
            if (track == null) {
                return false;
            }
            
            return ArtworkId == track.ArtworkId;
        }

        public virtual void Save ()
        {
        }
        
        public bool IsPlaying {
            get { return (IsPlayingMethod != null) ? IsPlayingMethod (this) : false; }
        }

        [Exportable (ExportName = "URI")]
        public virtual SafeUri Uri {
            get { return uri; }
            set { uri = value; }
        }

        [Exportable]
        public SafeUri MoreInfoUri {
            get { return more_info_uri; }
            set { more_info_uri = value; }
        }

        [Exportable]
        public virtual string MimeType {
            get { return mimetype; }
            set { mimetype = value; }
        }

        [Exportable]
        public virtual long FileSize {
            get { return filesize; }
            set { filesize = value; }
        }

        [Exportable (ExportName = "artist")]
        public virtual string ArtistName {
            get { return artist_name; }
            set { artist_name = value; }
        }

        [Exportable (ExportName = "album")]
        public virtual string AlbumTitle {
            get { return album_title; }
            set { album_title = value; }
        }

        [Exportable]
        public virtual string AlbumArtist {
            get { return album_artist ?? (IsCompilation ? Catalog.GetString ("Various Artists") : ArtistName); }
            set { album_artist = value; }
        }
        
        [Exportable]
        public virtual bool IsCompilation {
            get { return is_compilation; }
            set { is_compilation = value; }
        }

        [Exportable (ExportName = "name")]
        public virtual string TrackTitle {
            get { return track_title; }
            set { track_title = value; }
        }
        
        [Exportable]
        public virtual string MusicBrainzId {
            get { return musicbrainz_id; }
            set { musicbrainz_id = value; }
        }
        
        [Exportable]
        public virtual string ArtistMusicBrainzId {
            get { return null; }
        }
        
        [Exportable]
        public virtual string AlbumMusicBrainzId {
            get { return null; }
        }
        
        [Exportable]
        public virtual DateTime ReleaseDate {
            get { return release_date; }
            set { release_date = value; }
        }        
        
        public string DisplayArtistName { 
            get {
                string name = ArtistName == null ? null : ArtistName.Trim ();
                return String.IsNullOrEmpty (name)
                    ? Catalog.GetString ("Unknown Artist") 
                    : name; 
            } 
        }

        public string DisplayAlbumTitle { 
            get { 
                string title = AlbumTitle == null ? null : AlbumTitle.Trim ();
                return String.IsNullOrEmpty (title) 
                    ? Catalog.GetString ("Unknown Album") 
                    : title; 
            } 
        }

        public string DisplayTrackTitle { 
            get { 
                string title = TrackTitle == null ? null : TrackTitle.Trim ();
                return String.IsNullOrEmpty (title) 
                    ? Catalog.GetString ("Unknown Title") 
                    : title; 
            } 
        }     

        public string DisplayGenre { 
            get { 
                string genre = Genre == null ? null : Genre.Trim ();
                return String.IsNullOrEmpty (genre) 
                    ? Catalog.GetString ("Unknown Genre") 
                    : genre; 
            } 
        }     
        
        public virtual string ArtworkId { 
            get { return CoverArtSpec.CreateArtistAlbumId (AlbumArtist, AlbumTitle); }
        }

        [Exportable]
        public virtual string Genre {
            get { return genre; }
            set { genre = value; }
        }

        [Exportable]
        public virtual int TrackNumber {
            get { return track_number; }
            set { track_number = value; }
        }

        [Exportable]
        public virtual int TrackCount {
            get { return TrackNumber > track_count ? TrackNumber : track_count; }
            set { track_count = value; }
        }

        [Exportable]
        public virtual int DiscNumber {
            get { return disc_number; }
            set { disc_number = value; }
        }
        
        [Exportable]
        public virtual int DiscCount {
            get { return DiscNumber > disc_count ? DiscNumber : disc_count; }
            set { disc_count = value; }
        }

        [Exportable]
        public virtual int Year {
            get { return year; }
            set { year = value; }
        }

        [Exportable]
        public virtual string Composer {
            get { return composer; }
            set { composer = value; }
        }

        [Exportable]
        public virtual string Conductor {
            get { return conductor; }
            set { conductor = value; }
        }
        
        [Exportable]
        public virtual string Grouping {
            get { return grouping; }
            set { grouping = value; }
        }
        
        [Exportable]
        public virtual string Copyright {
            get { return copyright; }
            set { copyright = value; }
        }

        [Exportable]        
        public virtual string LicenseUri {
            get { return license_uri; }
            set { license_uri = value; }
        }

        [Exportable]
        public virtual string Comment {
            get { return comment; }
            set { comment = value; }
        }

        [Exportable]
        public virtual int Rating {
            get { return rating; }
            set { rating = value; }
        }
        
        [Exportable]
        public virtual int Bpm {
            get { return bpm; }
            set { bpm = value; }
        }

        [Exportable]
        public virtual int BitRate {
            get { return bit_rate; }
            set { bit_rate = value; }
        }

        [Exportable]
        public virtual int PlayCount {
            get { return play_count; }
            set { play_count = value; }
        }

        [Exportable]
        public virtual int SkipCount {
            get { return skip_count; }
            set { skip_count = value; }
        }

        [Exportable (ExportName = "length")]
        public virtual TimeSpan Duration {
            get { return duration; }
            set { duration = value; }
        }
        
        [Exportable]
        public virtual DateTime DateAdded {
            get { return date_added; }
            set { date_added = value; }
        }

        [Exportable]
        public virtual DateTime LastPlayed {
            get { return last_played; }
            set { last_played = value; }
        }

        [Exportable]
        public virtual DateTime LastSkipped {
            get { return last_skipped; }
            set { last_skipped = value; }
        }
        
        public virtual StreamPlaybackError PlaybackError {
            get { return playback_error; }
            set { playback_error = value; }
        }

        public void SavePlaybackError (StreamPlaybackError value)
        {
            if (PlaybackError != value) {
                PlaybackError = value;
                Save ();
            }
        }

        private bool can_save_to_database = true;
        public bool CanSaveToDatabase {
            get { return can_save_to_database; }
            set { can_save_to_database = value; }
        }
        
        private bool is_live = false;
        public bool IsLive {
            get { return is_live; }
            set { is_live = value; }
        }

        private bool can_play = true;
        public bool CanPlay {
            get { return can_play; }
            set { can_play = value; }
        }
        
        public virtual string MetadataHash {
            get {
                System.Text.StringBuilder sb = new System.Text.StringBuilder ();
                sb.Append (AlbumTitle);
                sb.Append (ArtistName);
                sb.Append ((int)Duration.TotalSeconds);
                sb.Append (Genre);
                sb.Append (TrackTitle);
                sb.Append (TrackNumber);
                sb.Append (Year);
                return Hyena.CryptoUtil.Md5Encode (sb.ToString (), System.Text.Encoding.UTF8);
            }
        }

        private TrackMediaAttributes media_attributes = TrackMediaAttributes.Default;
        
        [Exportable]
        public virtual TrackMediaAttributes MediaAttributes {
            get { return media_attributes; }
            set { media_attributes = value; }
        }
        
        public bool HasAttribute (TrackMediaAttributes attr)
        {
            return (MediaAttributes & attr) != 0;
        }

        protected void SetAttributeIf (bool condition, TrackMediaAttributes attr)
        {
            if (condition) {
                MediaAttributes |= attr;
            }
        }
        
        public string MediaTypeName {
            get {
                if (HasAttribute (TrackMediaAttributes.Podcast))
                    return Catalog.GetString ("Podcast");
                if (HasAttribute (TrackMediaAttributes.VideoStream))
                    return Catalog.GetString ("Video");
                if (HasAttribute (TrackMediaAttributes.Music))
                    return Catalog.GetString ("Song");
                return Catalog.GetString ("Item");
            }
        }
        
#region Exportable Properties
        
        public static void ExportableMerge (TrackInfo source, TrackInfo dest)
        {
            // Use the high level TrackInfo type if the source and dest types differ
            Type type = dest.GetType ();
            if (source.GetType () != type) {
                type = typeof (TrackInfo);
            }
            
            foreach (KeyValuePair<string, PropertyInfo> iter in GetExportableProperties (type)) {
                try {
                    PropertyInfo property = iter.Value;
                    if (property.CanWrite && property.CanRead) {
                        property.SetValue (dest, property.GetValue (source, null), null);
                    }
                } catch (Exception e) {
                    Log.Exception (e);
                }
            }
        }
        
        public IDictionary<string, object> GenerateExportable ()
        {
            Dictionary<string, object> dict = new Dictionary<string, object> ();

            foreach (KeyValuePair<string, PropertyInfo> property in GetExportableProperties (GetType ())) {
                object value = property.Value.GetValue (this, null);
                if (value == null) {
                    continue;
                }
                
                if (value is TimeSpan) {
                    value = ((TimeSpan)value).TotalSeconds;
                } else if (value is DateTime) {
                    DateTime date = (DateTime)value;
                    value = date == DateTime.MinValue ? 0l : DateTimeUtil.ToTimeT (date);
                } else if (value is SafeUri) {
                    value = ((SafeUri)value).AbsoluteUri;
                } else if (value is TrackMediaAttributes) {
                    value = value.ToString ();
                } else if (!(
                    value is ushort || value is short || 
                    value is uint || value is int ||
                    value is ulong || value is long ||
                    value is float || value is double ||
                    value is bool || value is string)) {
                    Log.WarningFormat ("Invalid property in {0} marked as [Exportable]: ({1} is a {2})", 
                        property.Value.DeclaringType, property.Value.Name, value.GetType ());
                    continue;
                }
                
                dict.Add (property.Key, value);
            }
            
            return dict;
        }
        
        private static Dictionary<Type, Dictionary<string, PropertyInfo>> exportable_properties;
        private static object exportable_properties_mutex = new object ();
        
        private static void FindExportableProperties (Type type)
        {
            lock (exportable_properties_mutex) {
                if (exportable_properties == null) {
                    exportable_properties = new Dictionary<Type, Dictionary<string, PropertyInfo>> ();
                } else if (exportable_properties.ContainsKey (type)) {
                    return;
                }
                
                // Build a stack of types to reflect
                Stack<Type> probe_types = new Stack<Type> ();
                Type probe_type = type;
                while (probe_type != null) {
                    probe_types.Push (probe_type);
                    if (probe_type == typeof (TrackInfo)) {
                        break;
                    }
                    probe_type = probe_type.BaseType;
                }
            
                // Iterate through all types
                while (probe_types.Count > 0) {
                    probe_type = probe_types.Pop ();
                    if (exportable_properties.ContainsKey (probe_type)) {
                        continue;
                    }
                    
                    Dictionary<string, PropertyInfo> properties = null;
                    
                    // Reflect the type for exportable properties
                    foreach (PropertyInfo property in probe_type.GetProperties (BindingFlags.Public | BindingFlags.Instance)) {
                        if (property.DeclaringType != probe_type) {
                            continue;
                        }
                        
                        object [] exportable_attrs = property.GetCustomAttributes (typeof (ExportableAttribute), true);
                        if (exportable_attrs == null || exportable_attrs.Length == 0) {
                            continue;
                        }
                        
                        string export_name = ((ExportableAttribute)exportable_attrs[0]).ExportName
                            ?? StringUtil.CamelCaseToUnderCase (property.Name, '-');
                        
                        if (String.IsNullOrEmpty (export_name) || (properties != null && properties.ContainsKey (export_name))) {
                            continue;
                        }
                        
                        if (properties == null) {
                            properties = new Dictionary<string, PropertyInfo> ();
                            exportable_properties.Add (probe_type, properties);
                        }
                        
                        properties.Add (export_name, property);
                    }
                    
                    // Merge properties in the type hierarchy through linking or aggregation
                    Type parent_type = probe_type.BaseType;
                    bool link = !exportable_properties.ContainsKey (probe_type);
                    
                    while (parent_type != null) {
                        Dictionary<string, PropertyInfo> parent_properties = null;
                        if (!exportable_properties.TryGetValue (parent_type, out parent_properties)) {
                            parent_type = parent_type.BaseType;
                            continue;
                        }
                        
                        if (link) {
                            // Link entire property set between types
                            exportable_properties.Add (probe_type, parent_properties);
                            return;
                        } else {
                            // Aggregate properties in parent sets
                            foreach (KeyValuePair<string, PropertyInfo> parent_property in parent_properties) {
                                properties.Add (parent_property.Key, parent_property.Value);
                            }
                        }
                        
                        parent_type = parent_type.BaseType;
                    }
                }
            }
        }
        
        private static IEnumerable<KeyValuePair<string, PropertyInfo>> GetExportableProperties (Type type)
        {
            FindExportableProperties (type);
            
            Dictionary<string, PropertyInfo> properties = null;
            if (exportable_properties.TryGetValue (type, out properties)) {
                foreach (KeyValuePair<string, PropertyInfo> property in properties) {
                    yield return property;
                }
            }
        }
        
#endregion

    }
}
