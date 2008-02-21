//
// Test.cs
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

using Lastfm.Data;

public class LastfmTest
{
    public static void Main (string [] args)
    {
        DataCore.CachePath = "test_lastfm_cache";
        DataCore.UserAgent = "Lastfm.dll test";

        string username = "RJ";

        LastfmUserData user = new LastfmUserData (username);

        ProfileEntry prof = user.Profile;
        //Console.WriteLine ("data url: {0}", prof.DataUrl);
        Console.WriteLine ("profile url: {0}", user.Profile.Url);
        Console.WriteLine ("real name: {0}", user.Profile.RealName);
        Console.WriteLine ("play count: {0}", prof.PlayCount);
        Console.WriteLine ("gender: {0}", prof.Gender);
        Console.WriteLine ("age: {0}", prof.Age);
        Console.WriteLine ("country: {0}", prof.Country);
        Console.WriteLine ("registered: {0}", prof.Registered);

        Console.WriteLine ("profile url: {0}", user.Profile.Url);

        LastfmData<UserTopArtist> top_artists = user.GetTopArtists (TopType.Overall);
        Console.WriteLine ("\nTop Artists ({0})", top_artists.Count);
        foreach (UserTopArtist artist in top_artists) {
            Console.WriteLine ("Artist: {0}\nPlays: {1}", artist.Name, artist.PlayCount);
        }

        foreach (TopTag tag in user.TopTags) {
            Console.WriteLine ("top tag: {0}", tag.Name);
        }
        user.TopTags.Refresh ();

        System.IO.Directory.Delete ("test_lastfm_cache", true);
    }
}
