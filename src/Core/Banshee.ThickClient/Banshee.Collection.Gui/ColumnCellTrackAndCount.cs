//
// ColumnCellTrackAndCount.cs
//
// Author:
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

using Mono.Unix;

using Hyena.Data.Gui;

namespace Banshee.Collection.Gui
{
    public class ColumnCellTrackAndCount : ColumnCellText
    {
        // Translators: this is {track number} of {track count}
        private static readonly string format = Catalog.GetString ("{0} of {1}");

        public ColumnCellTrackAndCount (string property, bool expand) : base (property, expand)
        {
            MinString = String.Format (format, 55, 55);
            MaxString = String.Format (format, 555, 555);
            RestrictSize = true;
        }

        protected override string GetText (object obj)
        {
            Banshee.Collection.TrackInfo track = BoundObjectParent as Banshee.Collection.TrackInfo;
            if (track == null || track.TrackNumber == 0) {
                return String.Empty;
            }
            return track.TrackCount != 0
                ? String.Format (format, track.TrackNumber, track.TrackCount)
                : track.TrackNumber.ToString ();
        }
    }
}
