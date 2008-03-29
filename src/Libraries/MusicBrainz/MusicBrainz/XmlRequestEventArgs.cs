/***************************************************************************
 *  XmlRequestEventArgs.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;

namespace MusicBrainz
{
    public sealed class XmlRequestEventArgs : EventArgs
    {
        public readonly string Uri;
        public readonly bool FromCache;
        
        public XmlRequestEventArgs(string uri, bool fromCache)
        {
            Uri = uri;
            FromCache = fromCache;
        }
    }
}
