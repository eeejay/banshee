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

using Banshee.Collection;
using Banshee.Configuration;
using Banshee.ServiceStack;

namespace Banshee.Database
{
    internal class SortKeyUpdater
    {
        public static void Update ()
        {
            string locale = CultureInfo.CurrentCulture.Name;
            if (locale != PreviousLocale) {
                Hyena.Log.DebugFormat ("Updating collation keys for locale {0} (was {1})", locale, PreviousLocale);
                ForceUpdate (locale);
            }
        }
        
        public static void ForceUpdate ()
        {
            ForceUpdate (CultureInfo.CurrentCulture.Name);
        }
        
        protected static void ForceUpdate (string new_locale)
        {
            ServiceManager.DbConnection.Execute (@"
                    BEGIN;
                    UPDATE CoreArtists SET
                        NameSortKey       = HYENA_COLLATION_KEY(COALESCE(NameSort, Name, ?)),
                        NameLowered       = HYENA_SEARCH_KEY(COALESCE(Name, ?));

                    UPDATE CoreAlbums SET
                        TitleSortKey      = HYENA_COLLATION_KEY(COALESCE(TitleSort, Title, ?)),
                        ArtistNameSortKey = HYENA_COLLATION_KEY(COALESCE(ArtistName, ?)),
                        TitleLowered      = HYENA_SEARCH_KEY(COALESCE(Title, ?)),
                        ArtistNameLowered = HYENA_SEARCH_KEY(COALESCE(ArtistName, ?));

                    UPDATE CoreTracks SET
                        TitleSortKey      = HYENA_COLLATION_KEY(COALESCE(TitleSort, Title, ?)),
                        TitleLowered      = HYENA_SEARCH_KEY(COALESCE(Title, ?));
                    COMMIT",
                ArtistInfo.UnknownArtistName, ArtistInfo.UnknownArtistName,
                AlbumInfo.UnknownAlbumTitle, ArtistInfo.UnknownArtistName,
                AlbumInfo.UnknownAlbumTitle, ArtistInfo.UnknownArtistName,
                TrackInfo.UnknownTitle, TrackInfo.UnknownTitle
            );

            DatabaseConfigurationClient.Client.Set<string> ("SortKeyLocale", new_locale);
        }
        
        protected static string PreviousLocale {
            get {
                return DatabaseConfigurationClient.Client.Get<string> ("SortKeyLocale", "");
            }
        }
    }
}
