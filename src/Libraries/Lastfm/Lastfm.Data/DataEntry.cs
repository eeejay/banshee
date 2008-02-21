//
// DataEntry.cs
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
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace Lastfm.Data
{
    public class DataEntry
    {
        private XmlElement root;
        public XmlElement Root {
            get { return root; }
            set { root = value; }
        }

        public DataEntry ()
        {
        }

        public DataEntry (XmlElement root)
        {
            this.root = root;
        }

        public T Get<T> (string name)
        {
            try {
                XmlElement node = root[name];
                if (node != null) {
                    return (T) Convert.ChangeType (node.InnerText, typeof(T));
                } else if (root.HasAttribute (name)) {
                    return (T) Convert.ChangeType (root.GetAttribute (name), typeof(T));
                }
            } catch (Exception) {}
            return default(T);
        }
    }

    // Generic types
    public class NamedEntry : DataEntry
    {
        public string Name              { get { return Get<string>   ("name"); } }
        public string Url               { get { return Get<string>   ("url"); } }
    }

    public class TopTag : NamedEntry
    {
        public int Count { get { return Get<int>   ("count"); } }
    }

    public class RssEntry : DataEntry
    {
        public string Title             { get { return Get<string>   ("title"); } }
        public string Link              { get { return Get<string>   ("link"); } }
        public DateTime PublicationDate { get { return Get<DateTime> ("pubDate"); } }
        public string Guid              { get { return Get<string>   ("guid"); } }
        public string Description       { get { return Get<string>   ("description"); } }
    }

    // User-specific types

    public class ProfileEntry : DataEntry
    {
        public string Url               { get { return Get<string>   ("url"); } }
        public string RealName          { get { return Get<string>   ("realname"); } }
        public string Gender            { get { return Get<string>   ("gender"); } }
        public string Country           { get { return Get<string>   ("country"); } }
        public string AvatarUrl         { get { return Get<string>   ("avatar"); } }
        public string IconUrl           { get { return Get<string>   ("icon"); } }
        public DateTime Registered      { get { return Get<DateTime> ("registered"); } }
        public int Age                  { get { return Get<int>      ("age"); } }
        public int PlayCount            { get { return Get<int>      ("playcount"); } }
    }

    public class UserTopArtist : NamedEntry
    {
        public string MbId              { get { return Get<string>   ("mbid"); } }
        public int Rank                 { get { return Get<int>      ("rank"); } }
        public int PlayCount            { get { return Get<int>      ("playcount"); } }
        public string ThumbnailUrl      { get { return Get<string>   ("thumbnail"); } }
        public string ImageUrl          { get { return Get<string>   ("image"); } }
    }

    public class UserTopAlbum : NamedEntry
    {
        public string Artist            { get { return Get<string>   ("artist"); } }
        public string MbId              { get { return Get<string>   ("mbid"); } }
        public int Rank                 { get { return Get<int>      ("rank"); } }
        public int PlayCount            { get { return Get<int>      ("playcount"); } }
    }

    public class UserTopTrack : NamedEntry
    {
        public string Artist            { get { return Get<string>   ("artist"); } }
        public int Rank                 { get { return Get<int>      ("rank"); } }
        public int PlayCount            { get { return Get<int>      ("playcount"); } }
    }

    public class Friend : DataEntry
    {
        public string UserName          { get { return Get<string>   ("username"); } }
        public string Url               { get { return Get<string>   ("url"); } }
        public string ImageUrl          { get { return Get<string>   ("image"); } }
    }

    public class Neighbor : Friend
    {
        public double Match             { get { return Get<double>   ("match"); } }
        public int MatchAsInt           { get { return (int) Math.Round (Match); } }
    }

    public class RecentTrack : NamedEntry
    {
        public string Artist            { get { return Get<string>   ("artist"); } }
        public string Album             { get { return Get<string>   ("album"); } }
        public DateTime Date            { get { return Get<DateTime> ("date"); } }
    }

    public class DateRangeEntry : DataEntry
    {
        public DateTime From            { get { return Get<DateTime> ("from"); } }
        public DateTime To              { get { return Get<DateTime> ("to"); } }
    }

    public class ArtistChartEntry : NamedEntry
    {
        public string MbId              { get { return Get<string>   ("mbid"); } }
        public int PlayCount            { get { return Get<int>      ("playcount"); } }
        public int ChartPosition        { get { return Get<int>      ("chartposition"); } }
    }

    public class RecommendedArtistEntry : NamedEntry
    {
        public string MbId              { get { return Get<string>   ("mbid"); } }
    }

    public class EventEntry : RssEntry
    {
        public DateTime Begins          { get { return Get<DateTime> ("xcal:dtstart"); } }
        public DateTime Ends            { get { return Get<DateTime> ("xcal:dtend"); } }
    }

    public class TasteArtist : RssEntry
    {
        public string Name              { get { return Get<string>   ("name"); } }
    }

    public class TasteEntry : DataEntry
    {
        public double Score             { get { return Get<double>   ("score"); } }

        private DataEntryCollection<TasteArtist> artists;
        public DataEntryCollection<TasteArtist> Artists {
            get {
                if (artists == null) {
                    artists = new DataEntryCollection<TasteArtist> (Root ["commonArtists"].ChildNodes);
                }
                return artists;
            }
        }
    }

    // Artist entries
    public class SimilarArtist : NamedEntry
    {
        public double Match             { get { return Get<double>   ("match"); } }
        public int MatchAsInt           { get { return (int) Math.Round (Match); } }
    }

    public class ArtistFan : Friend
    {
        public int Weight               { get { return Get<int>      ("weight"); } }
    }

    public class ArtistTopTrack : NamedEntry
    {
        public int Reach                { get { return Get<int>      ("reach"); } }
    }

    public class ArtistTopAlbum : NamedEntry
    {
        public int Reach                { get { return Get<int>      ("reach"); } }
    }
}
