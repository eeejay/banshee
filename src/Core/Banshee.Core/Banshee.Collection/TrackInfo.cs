
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

        public virtual SafeUri Uri {
            get { return uri; }
            set { uri = value; }
        }

        public SafeUri MoreInfoUri {
            get { return more_info_uri; }
            set { more_info_uri = value; }
        }

        public virtual string MimeType {
            get { return mimetype; }
            set { mimetype = value; }
        }

        public virtual long FileSize {
            get { return filesize; }
            set { filesize = value; }
        }

        public virtual string ArtistName {
            get { return artist_name; }
            set { artist_name = value; }
        }

        public virtual string AlbumTitle {
            get { return album_title; }
            set { album_title = value; }
        }

        public virtual string AlbumArtist {
            get { return album_artist ?? (IsCompilation ? Catalog.GetString ("Various Artists") : ArtistName); }
            set { album_artist = value; }
        }
        
        public virtual bool IsCompilation {
            get { return is_compilation; }
            set { is_compilation = value; }
        }

        public virtual string TrackTitle {
            get { return track_title; }
            set { track_title = value; }
        }
        
        public virtual string MusicBrainzId {
            get { return musicbrainz_id; }
            set { musicbrainz_id = value; }
        }
        
        public virtual string ArtistMusicBrainzId {
            get { return null; }
        }
        
        public virtual string AlbumMusicBrainzId {
            get { return null; }
        }
        
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

        public virtual string Genre {
            get { return genre; }
            set { genre = value; }
        }

        public virtual int TrackNumber {
            get { return track_number; }
            set { track_number = value; }
        }

        public virtual int TrackCount {
            get { return TrackNumber > track_count ? TrackNumber : track_count; }
            set { track_count = value; }
        }

        public virtual int DiscNumber {
            get { return disc_number; }
            set { disc_number = value; }
        }

        public virtual int DiscCount {
            get { return DiscNumber > disc_count ? DiscNumber : disc_count; }
            set { disc_count = value; }
        }

        public virtual int Year {
            get { return year; }
            set { year = value; }
        }

        public virtual string Composer {
            get { return composer; }
            set { composer = value; }
        }

        public virtual string Conductor {
            get { return conductor; }
            set { conductor = value; }
        }

        public virtual string Grouping {
            get { return grouping; }
            set { grouping = value; }
        }

        public virtual string Copyright {
            get { return copyright; }
            set { copyright = value; }
        }

        public virtual string LicenseUri {
            get { return license_uri; }
            set { license_uri = value; }
        }

        public virtual string Comment {
            get { return comment; }
            set { comment = value; }
        }

        public virtual int Rating {
            get { return rating; }
            set { rating = value; }
        }
        
        public virtual int Bpm {
            get { return bpm; }
            set { bpm = value; }
        }

        public virtual int BitRate {
            get { return bit_rate; }
            set { bit_rate = value; }
        }

        public virtual int PlayCount {
            get { return play_count; }
            set { play_count = value; }
        }

        public virtual int SkipCount {
            get { return skip_count; }
            set { skip_count = value; }
        }

        public virtual TimeSpan Duration {
            get { return duration; }
            set { duration = value; }
        }
        
        public virtual DateTime DateAdded {
            get { return date_added; }
            set { date_added = value; }
        }

        public virtual DateTime LastPlayed {
            get { return last_played; }
            set { last_played = value; }
        }

        public virtual DateTime LastSkipped {
            get { return last_skipped; }
            set { last_skipped = value; }
        }
        
        public virtual StreamPlaybackError PlaybackError {
            get { return playback_error; }
            set { playback_error = value; }
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
        public virtual TrackMediaAttributes MediaAttributes {
            get { return media_attributes; }
            set { media_attributes = value; }
        }
        
        public bool IsMedia (TrackMediaAttributes attr)
        {
            return (MediaAttributes & attr) != 0;
        }
        
        public string MediaTypeName {
            get {
                if (IsMedia (TrackMediaAttributes.Podcast))
                    return Catalog.GetString ("Podcast");
                if (IsMedia (TrackMediaAttributes.VideoStream))
                    return Catalog.GetString ("Video");
                if (IsMedia (TrackMediaAttributes.Music))
                    return Catalog.GetString ("Song");
                return Catalog.GetString ("Item");
            }
        }

        // Generates a{sv} of self according to http://wiki.xmms2.xmms.se/index.php/Media_Player_Interfaces#.22Metadata.22
        public IDictionary<string, object> GenerateExportable ()
        {
            Dictionary<string, object> dict = new Dictionary<string, object> ();
            
            // Properties specified by the XMMS2 player spec
            dict.Add ("URI", Uri == null ? String.Empty : Uri.AbsoluteUri);
            dict.Add ("length", Duration.TotalSeconds);
            dict.Add ("name", TrackTitle);
            dict.Add ("artist", ArtistName);
            dict.Add ("album", AlbumTitle);
            
            // Our own
            dict.Add ("track-number", TrackNumber);
            dict.Add ("track-count", TrackCount);
            dict.Add ("disc", DiscNumber);
            dict.Add ("year", year);
            dict.Add ("rating", rating);
            
            return dict;
        }
    }
}
