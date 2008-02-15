//
// Profile.cs
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
    // http://ws.audioscrobbler.com/1.0/user/RJ/profile.xml
    // <profile id="1000002" cluster="2" username="RJ">
    // <url>http://www.last.fm/user/RJ/</url>
    // <realname>Richard Jones </realname>
    // <mbox_sha1sum>0bf0949e67cffa83c66fd806e817dda3083c4d8b</mbox_sha1sum>
    // <registered unixtime="1037793040">Nov 20, 2002</registered>
    // <age>25</age>
    // <gender>m</gender>
    // <country>United Kingdom</country>
    // <playcount>50644</playcount>
    // <avatar>http://userserve-ak.last.fm/serve/160/622321.jpg</avatar>
    // <icon>http://cdn.last.fm/depth/global/icon_staff.gif</icon>
    // </profile>

    public class Profile : LastfmData
    {
        private DataEntry profile;
        private string username;

        public Profile (string username) : base (String.Format ("user/{0}/profile.xml", username))
        {
            this.username = username;
            profile = new DataEntry (doc["profile"]);
        }

        public string Username          { get { return username; } }
        public string Url               { get { return profile.Get<string>   ("url"); } }
        public string RealName          { get { return profile.Get<string>   ("realname"); } }
        public string Gender            { get { return profile.Get<string>   ("gender"); } }
        public string Country           { get { return profile.Get<string>   ("country"); } }
        public string AvatarUrl         { get { return profile.Get<string>   ("avatar"); } }
        public string IconUrl           { get { return profile.Get<string>   ("icon"); } }
        public DateTime Registered      { get { return profile.Get<DateTime> ("registered"); } }
        public int Age                  { get { return profile.Get<int>      ("age"); } }
        public int PlayCount            { get { return profile.Get<int>      ("playcount"); } }
    }
}

