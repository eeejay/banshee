//
// LastfmUserData.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

namespace Lastfm.Data
{
    public enum TopType {
        Overall,
        ThreeMonth,
        SixMonth,
        TwelveMonth,
    }

    public class LastfmUserData
    {
        protected Dictionary <string, object> cache = new Dictionary <string, object> ();

        protected string username;
        public string Username {
            get { return username; }
        }

        public LastfmUserData (string username)
        {
            this.username = username;
        }

        protected LastfmData<T> Get<T> (string fragment) where T : DataEntry
        {
            return Get<T> (fragment, null);
        }

        protected LastfmData<T> Get<T> (string fragment, string xpath) where T : DataEntry
        {
            if (cache.ContainsKey (fragment)) {
                return (LastfmData<T>) cache [fragment];
            }

            LastfmData<T> obj = new LastfmData<T> (String.Format ("user/{0}/{1}", username, fragment), xpath);
            cache [fragment] = obj;
            return obj;
        }

#region Public Methods

        public LastfmData<UserTopArtist> GetTopArtists (TopType type)
        {
            return Get<UserTopArtist> (AppendType ("topartists.xml", type));
        }

        public LastfmData<UserTopAlbum> GetTopAlbums (TopType type)
        {
            return Get<UserTopAlbum> (AppendType ("topalbums.xml", type));
        }

        public LastfmData<UserTopTrack> GetTopTracks (TopType type)
        {
            return Get<UserTopTrack> (AppendType ("toptracks.xml", type));
        }

        public LastfmData<TopTag> GetTopTagsForArtist (string artist)
        {
            return Get<TopTag> (String.Format ("artisttags.xml?artist={0}", artist));
        }

        public LastfmData<TopTag> GetTopTagsForAlbum (string artist, string album)
        {
            return Get<TopTag> (String.Format ("albumtags.xml?artist={0}&album={1}", artist, album));
        }

        public LastfmData<TopTag> GetTopTagsForTrack (string artist, string track)
        {
            return Get<TopTag> (String.Format ("tracktags.xml?artist={0}&track={1}", artist, track));
        }

        public TasteEntry GetTasteOMeter (string other_username)
        {
            return (Get<TasteEntry> (String.Format ("tasteometer.xml?with={0}", other_username)))[0];
        }

#endregion

#region Public Properties

        public ProfileEntry Profile {
            get { return (Get<ProfileEntry> ("profile.xml", "profile"))[0]; }
        }

        public LastfmData<TopTag> TopTags {
            get { return Get<TopTag> ("tags.xml"); }
        }

        // ?showtracks=1
        public LastfmData<Friend> Friends {
            get { return Get<Friend> ("friends.xml"); }
        }

        public LastfmData<Neighbor> Neighbors {
            get { return Get<Neighbor> ("neighbours.xml"); }
        }

        public LastfmData<RecentTrack> RecentTracks {
            get { return Get<RecentTrack> ("recenttracks.xml"); }
        }

        public LastfmData<RecentTrack> RecentLovedTracks {
            get { return Get<RecentTrack> ("recentlovedtracks.xml"); }
        }

        public LastfmData<RecentTrack> RecentBannedTracks {
            get { return Get<RecentTrack> ("recentbannedtracks.xml"); }
        }

        public LastfmData<RssEntry> RecentJournalEntries {
            get { return Get<RssEntry> ("journals.rss"); }
        }

        public LastfmData<DateRangeEntry> WeeklyChartList{
            get { return Get<DateRangeEntry> ("weeklychartlist.xml"); }
        }

        public LastfmData<ArtistChartEntry> WeeklyArtistChart {
            get { return Get<ArtistChartEntry> ("weeklyartistchart.xml"); }
        }

        public LastfmData<RecommendedArtistEntry> RecommendedArtists {
            get { return Get<RecommendedArtistEntry> ("systemrecs.xml"); }
        }

        public LastfmData<RssEntry> ManualRecommendations {
            get { return Get<RssEntry> ("manualrecs.rss"); }
        }

        public LastfmData<EventEntry> FriendsEvents {
            get { return Get<EventEntry> ("friendevents.rss"); }
        }

        public LastfmData<EventEntry> RecommendedEvents {
            get { return Get<EventEntry> ("eventsysrecs.rss"); }
        }

#endregion

        protected static string AppendType (string fragment, TopType type)
        {
            return String.Format ("{0}?type={1}", fragment, TopTypeToParam (type));
        }

        protected static string TopTypeToParam (TopType type)
        {
            switch (type) {
                case TopType.Overall:       return "overall";
                case TopType.ThreeMonth:    return "3month";
                case TopType.SixMonth:      return "6month";
                case TopType.TwelveMonth:   return "12month";
            }
            return null;
        }
    }
}

