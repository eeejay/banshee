//
// PlaybackErrorQueryValue.cs
//
// Author:
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Alexander Kojevnikov
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

using Hyena.Query;

using Banshee.Streaming;

namespace Banshee.Query
{
    public class PlaybackErrorQueryValue : EnumQueryValue
    {
        private static AliasedObjectSet<EnumQueryValueItem> items = new AliasedObjectSet<EnumQueryValueItem>(
            new EnumQueryValueItem (
                (int)StreamPlaybackError.None, "None", Catalog.GetString ("None"),
                //Translators: These are unique strings for playback errors. Please, no spaces. Blank ok.
                Catalog.GetString ("None"), Catalog.GetString ("none"), Catalog.GetString ("no"),
                "None", "none", "no"),
            new EnumQueryValueItem (
                (int)StreamPlaybackError.ResourceNotFound, "ResourceNotFound", Catalog.GetString ("Resource Not Found"),
                //Translators: These are unique strings for playback errors. Please, no spaces. Blank ok.
                Catalog.GetString ("ResourceNotFound"), Catalog.GetString ("missing"), Catalog.GetString ("notfound"),
                "ResourceNotFound", "missing", "notfound"),
            new EnumQueryValueItem (
                (int)StreamPlaybackError.CodecNotFound, "CodecNotFound", Catalog.GetString ("CodecNotFound"),
                //Translators: These are unique strings for playback errors. Please, no spaces. Blank ok.
                Catalog.GetString ("CodecNotFound"), Catalog.GetString ("nocodec"),
                "CodecNotFound", "nocodec"),
            new EnumQueryValueItem (
                (int)StreamPlaybackError.Drm, "Drm", Catalog.GetString ("Drm"),
                //Translators: These are unique strings for playback errors. Please, no spaces. Blank ok.
                Catalog.GetString ("Drm"), Catalog.GetString ("drm"),
                "Drm", "drm"),
            new EnumQueryValueItem (
                (int)StreamPlaybackError.Unknown, "Unknown", Catalog.GetString ("Unknown"),
                //Translators: These are unique strings for playback errors. Please, no spaces. Blank ok.
                Catalog.GetString ("Unknown"), Catalog.GetString ("unknown"),
                "Unknown", "unknown")
        );

        public override IEnumerable<EnumQueryValueItem> Items {
            get { return items; }
        }
    }
}
