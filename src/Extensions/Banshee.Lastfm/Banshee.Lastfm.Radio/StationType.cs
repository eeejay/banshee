//
// StationType.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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

namespace Banshee.Lastfm.Radio
{
    public class StationType
    {
        public string Name, Label;
        public string ArgLabel;
        public bool SubscribersOnly;
        public string IconName;
        private string station_format;

        private StationType (string name, string label, string argLabel, string stationFormat, string iconName, bool subscribersOnly)
        {
            Name = name;
            Label = label;
            ArgLabel = argLabel;
            station_format = stationFormat;
            IconName = iconName;
            SubscribersOnly = subscribersOnly;
        }

        public string GetStationFor (string arg)
        {
            string url = String.Format (station_format, arg);
            return url.StartsWith ("lastfm://") ? url : "lastfm://" + url;
        }

        public override string ToString ()
        {
            return Name;
        }

        public static StationType FindByName (string name)
        {
            foreach (StationType type in Types) {
                if (type.Name == name)
                    return type;
            }
            return null;
        }

        public static StationType FindByLabel (string label)
        {
            foreach (StationType type in Types) {
                if (type.Label == label)
                    return type;
            }
            return null;
        }

        public static StationType Recommended = new StationType (
            "Recommended",
            Catalog.GetString ("Recommended"),
            Catalog.GetString ("For User:"),
            "user/{0}/recommended/100",
            "lastfm-recommended",
            false
        );

        public static StationType Personal = new StationType (
            "Personal",
            Catalog.GetString ("Personal"),
            Catalog.GetString ("For User:"),
            "user/{0}/personal",
            "lastfm-personal",
            false
        );

        public static StationType Loved = new StationType (
            "Loved",
            Catalog.GetString ("Loved"),
            Catalog.GetString ("By User:"),
            "user/{0}/loved",
            "emblem-favorite",
            true
        );

        public static StationType Neighbor = new StationType (
            "Neighbor",
            Catalog.GetString ("Neighbors"),
            Catalog.GetString ("Of User:"),
            "user/{0}/neighbours",
            "system-users",
            false
        );

        public static StationType Group = new StationType (
            "Group",
            Catalog.GetString ("Group"),
            Catalog.GetString ("Group Name:"),
            "group/{0}",
            "system-users",
            false
        );

        public static StationType Tag = new StationType (
            "Tag",
            Catalog.GetString ("Tag"),
            Catalog.GetString ("Tag Name:"),
            "globaltags/{0}",
            "lastfm-tag",
            false
        );

        public static StationType Fan = new StationType (
            "Fan",
            Catalog.GetString ("Fan"),
            Catalog.GetString ("Fans of:"),
            "artist/{0}/fans",
            "system-users",
            false
        );

        public static StationType Similar = new StationType (
            "Similar",
            Catalog.GetString ("Similar"),
            Catalog.GetString ("Similar to:"),
            "artist/{0}/similarartists",
            "lastfm-personal",
            false
        );

        public static StationType LastfmUrl = new StationType (
            "LastfmUrl",
            Catalog.GetString ("lastfm:// URL"),
            Catalog.GetString ("lastfm://"),
            "{0}",
            "lastfm-personal",
            false
        );

        public static StationType [] Types = new StationType [] {
            Recommended, Personal, Loved, Neighbor, Group, Tag, Fan, Similar, LastfmUrl
        };
    }
}
