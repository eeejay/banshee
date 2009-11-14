//
// IMetadataProvider.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using System.Collections.ObjectModel;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Streaming;

namespace Banshee.Metadata
{
    public delegate void MetadataLookupResultHandler(object o, MetadataLookupResultArgs args);

    public class MetadataLookupResultArgs : EventArgs
    {
        private IBasicTrackInfo track;
        private ReadOnlyCollection<StreamTag> tags;

        public MetadataLookupResultArgs(IBasicTrackInfo track, ReadOnlyCollection<StreamTag> tags)
        {
            this.track = track;
            this.tags = tags;
        }

        public IBasicTrackInfo Track {
            get { return track; }
        }

        public ReadOnlyCollection<StreamTag> ResultTags {
            get { return tags; }
        }
    }

    public interface IMetadataProvider
    {
        event MetadataLookupResultHandler HaveResult;

        IMetadataLookupJob CreateJob(IBasicTrackInfo track);

        void Lookup(IBasicTrackInfo track);
        void Cancel(IBasicTrackInfo track);
        void Cancel();
    }
}
