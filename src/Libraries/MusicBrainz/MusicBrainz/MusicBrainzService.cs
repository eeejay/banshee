/***************************************************************************
 *  MusicBrainzService.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;
using System.Net.Cache;

namespace MusicBrainz
{
    public static class MusicBrainzService
    {
        public static string ProviderUrl = @"http://musicbrainz.org/ws/1/";
        public static RequestCachePolicy CachePolicy;
        public static event EventHandler<XmlRequestEventArgs> XmlRequest;
        
        internal static void OnXmlRequest (string url, bool fromCache)
        {
            EventHandler<XmlRequestEventArgs> handler = XmlRequest;
            if (handler != null) handler (null, new XmlRequestEventArgs (url, fromCache));
        }
    }
}
