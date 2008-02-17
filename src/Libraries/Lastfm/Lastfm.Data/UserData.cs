//
// UserData.cs
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

namespace Lastfm.Data
{
    public enum TopType {
        Overall,
        ThreeMonth,
        SixMonth,
        TwelveMonth,
    }

    public class UserData<T> : LastfmData<T> where T : DataEntry
    {
        protected string username;
        public string Username {
            get { return username; }
        }

        public UserData (string username, string fragment) : this (username, fragment, null)
        {
        }

        public UserData (string username, string fragment, string xpath) : base (String.Format ("user/{0}/{1}", username, fragment), xpath)
        {
            this.username = username;
        }
    }

    public sealed class UserData
    {
        public static ProfileEntry GetProfile (string username)
        {
            return (new UserData<ProfileEntry> (username, "profile.xml", "/"))[0];
        }

        public static UserData<UserTopArtist> GetTopArtists (string username, TopType type)
        {
            return new UserData<UserTopArtist> (username, AppendType ("topartists.xml", type));
        }

        public static UserData<UserTopAlbum> GetTopAlbums (string username, TopType type)
        {
            return new UserData<UserTopAlbum> (username, AppendType ("topalbums.xml", type));
        }

        public static UserData<UserTopTrack> GetTopTracks (string username, TopType type)
        {
            return new UserData<UserTopTrack> (username, AppendType ("toptracks.xml", type));
        }

        public static UserData<TopTag> GetTopTags (string username)
        {
            return new UserData<TopTag> (username, "tags.xml");
        }

        public static UserData<TopTag> GetTopTagsForArtist (string username, string artist)
        {
            return new UserData<TopTag> (username, String.Format ("artisttags.xml?artist={0}", artist));
        }

        public static UserData<TopTag> GetTopTagsForAlbum (string username, string artist, string album)
        {
            return new UserData<TopTag> (username, String.Format ("albumtags.xml?artist={0}&album={1}", artist, album));
        }

        public static UserData<TopTag> GetTopTagsForTrack (string username, string artist, string track)
        {
            return new UserData<TopTag> (username, String.Format ("tracktags.xml?artist={0}&track={1}", artist, track));
        }

        public static UserData<Friend> GetFriends (string username)
        {
            return new UserData<Friend> (username, "friends.xml");
        }

        public static UserData<Neighbor> GetNeighbors (string username)
        {
            return new UserData<Neighbor> (username, "neighbours.xml");
        }

        public static UserData<RecentTrack> GetRecentTracks (string username)
        {
            return new UserData<RecentTrack> (username, "recenttracks.xml");
        }

        public static UserData<RecentTrack> GetRecentLovedTracks (string username)
        {
            return new UserData<RecentTrack> (username, "recentlovedtracks.xml");
        }

        public static UserData<RecentTrack> GetRecentBannedTracks (string username)
        {
            return new UserData<RecentTrack> (username, "recentbannedtracks.xml");
        }

        public static UserData<RssEntry> GetRecentJournalEntries (string username)
        {
            return new UserData<RssEntry> (username, "journals.rss");
        }

        public static UserData<DateRangeEntry> GetWeeklyChartList (string username)
        {
            return new UserData<DateRangeEntry> (username, "weeklychartlist.xml");
        }

        public static UserData<ArtistChartEntry> GetWeeklyArtistChart (string username)
        {
            return new UserData<ArtistChartEntry> (username, "weeklyartistchart.xml");
        }

        public static UserData<RecommendedArtistEntry> GetRecommendedArtists (string username)
        {
            return new UserData<RecommendedArtistEntry> (username, "systemrecs.xml");
        }

        public static UserData<RssEntry> GetManualRecommendations (string username)
        {
            return new UserData<RssEntry> (username, "manualrecs.rss");
        }

        public static UserData<EventEntry> GetFriendsEvents (string username)
        {
            return new UserData<EventEntry> (username, "friendevents.rss");
        }

        public static UserData<EventEntry> GetRecommendedEvents (string username)
        {
            return new UserData<EventEntry> (username, "eventsysrecs.rss");
        }

        public static TasteEntry GetTasteOMeter (string username, string other_username)
        {
            return (new UserData<TasteEntry> (username, String.Format ("tasteometer.xml?with={0}", other_username)))[0];
        }

        private static string AppendType (string fragment, TopType type)
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

