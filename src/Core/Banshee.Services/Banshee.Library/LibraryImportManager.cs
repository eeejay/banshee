// 
// LibraryImportManager.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.IO;

using Mono.Unix;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Streaming;

namespace Banshee.Library
{
    public class LibraryImportManager : DatabaseImportManager, IService
    {
        public LibraryImportManager () : base (DefaultTrackPrimarySourceChooser)
        {
            can_copy_to_library = true;
        }

        protected override ErrorSource ErrorSource {
            get { return ServiceManager.SourceManager.MusicLibrary.ErrorSource; }
        }

        protected static PrimarySource DefaultTrackPrimarySourceChooser (DatabaseTrackInfo track)
        {
            if ((track.MediaAttributes & TrackMediaAttributes.VideoStream) != 0) {
                return ServiceManager.SourceManager.VideoLibrary;
            } else {
                return ServiceManager.SourceManager.MusicLibrary;
            }
        }
        
        string IService.ServiceName {
            get { return "LibraryImportManager"; }
        }
    }
}
