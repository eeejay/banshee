//
// EditorTrackInfo.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using TagLib;

using Banshee.Streaming;
using Banshee.Collection;

namespace Banshee.Gui.TrackEditor
{
    public class EditorTrackInfo : TrackInfo
    {
        private TagLib.File taglib_file;
        private bool taglib_file_exists = true;
        
        public EditorTrackInfo (TrackInfo sourceTrack)
        {
            source_track = sourceTrack;
            TrackInfo.ExportableMerge (source_track, this);
        }
        
        private bool changed;
        public bool Changed {
            get { return changed; }
            set { changed = value; }
        }
        
        private int editor_index;
        public int EditorIndex {
            get { return editor_index; }
            set { editor_index = value; }
        }
        
        private int editor_count;
        public int EditorCount {
            get { return editor_count; }
            set { editor_count = value; }
        }
        
        private TrackInfo source_track;
        public TrackInfo SourceTrack {
            get { return source_track; }
        }
        
        public TagLib.File TaglibFile {
            get {
                if (taglib_file != null) {
                    return taglib_file;
                } else if (!taglib_file_exists) {
                    return null;
                }
                    
                try {
                    taglib_file = StreamTagger.ProcessUri (Uri);
                    if (taglib_file != null) {
                        return taglib_file;
                    }
                } catch (Exception e) {
                    Hyena.Log.Exception ("Cannot load TagLib file", e);
                }
                
                taglib_file_exists = false;
                return null;
            }
        }
    }
}
