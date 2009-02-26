//
// SortKeyUpdater.cs
//
// Author:
//   John Millikin <jmillikin@gmail.com>
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
using System.Globalization;
using Banshee.Configuration;
using Banshee.ServiceStack;

namespace Banshee.Database
{
    internal class SortKeyUpdater
    {
        public static void Update ()
        {
            string locale = CultureInfo.CurrentCulture.Name;
            Hyena.Log.DebugFormat ("locale = {0}, previous = {1}", locale, PreviousLocale);
            if (locale != PreviousLocale) {
                ForceUpdate (locale);
            }
        }
        
        public static void ForceUpdate ()
        {
            ForceUpdate (CultureInfo.CurrentCulture.Name);
        }
        
        protected static void ForceUpdate (string new_locale)
        {
            BansheeDbConnection db = ServiceManager.DbConnection;
            db.Execute ("BEGIN");
            db.Execute (@"UPDATE CoreArtists SET NameSortKey = HYENA_COLLATION_KEY(IFNULL(NameSort,Name))");
            db.Execute (@"UPDATE CoreAlbums SET TitleSortKey = HYENA_COLLATION_KEY(IFNULL(TitleSort,Title)),
                                                ArtistNameSortKey = HYENA_COLLATION_KEY(IFNULL(ArtistNameSort, ArtistName))");
            db.Execute (@"UPDATE CoreTracks SET TitleSortKey = HYENA_COLLATION_KEY(IFNULL(TitleSort,Title))");
            
            DatabaseConfigurationClient.Client.Set<string> ("SortKeyLocale", new_locale);
            db.Execute ("COMMIT");
        }
        
        protected static string PreviousLocale {
            get {
                return DatabaseConfigurationClient.Client.Get<string> ("SortKeyLocale", "");
            }
        }
    }
}
