//
// DownloadStatusFilterModel.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Hyena.Data;

using Banshee.Database;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Podcasting.Data;

using Migo.Syndication;

namespace Banshee.Podcasting.Gui
{
    public enum DownloadedStatusFilter
    {
        Both,
        Downloaded,
        NotDownloaded
    }

    public class DownloadStatusFilterModel : FilterListModel<DownloadedStatusFilter>
    {
        public DownloadStatusFilterModel (DatabaseTrackListModel trackModel) : base (trackModel)
        {
            // By default, select All items
            Selection.Clear (false);
            Selection.QuietSelect (0);
        }
        
        public override void Reload (bool notify)
        {
            if (notify)
                OnReloaded ();
        }
        
        public override void Clear ()
        {
        }
        
        public override DownloadedStatusFilter this [int index] {
            get {
                switch (index) {
                    case 1:    return DownloadedStatusFilter.Downloaded;
                    case 2:    return DownloadedStatusFilter.NotDownloaded;
                    case 0:
                    default:   return DownloadedStatusFilter.Both;
                }
            }
        }
        
        public override int Count {
            get { return 3; }
        }

        public override string GetSqlFilter ()
        {
            if (Selection.AllSelected)
                return null;
            else if (Selection.Contains (1))
                return String.Format ("PodcastEnclosures.DownloadStatus = {0}", (int)FeedDownloadStatus.Downloaded);
            else
                return String.Format ("PodcastEnclosures.DownloadStatus != {0}", (int)FeedDownloadStatus.Downloaded);
        }
    }
}
