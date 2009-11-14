//
// PlaylistFormatDescription.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

namespace Banshee.Playlists.Formats
{
    public delegate bool PlaylistFormatMagicHandler(System.IO.StreamReader reader);

    public class PlaylistFormatDescription
    {
        private Type type;
        private string name;
        private string extension;
        private string [] mimetypes;
        private PlaylistFormatMagicHandler magic_handler;

        public PlaylistFormatDescription(Type type, PlaylistFormatMagicHandler magic_handler, string name, string extension, string [] mimetypes)
        {
            this.type = type;
            this.magic_handler = magic_handler;
            this.name = name;
            this.extension = extension;
            this.mimetypes = mimetypes;
        }

        public Type Type {
            get { return type; }
        }

        public PlaylistFormatMagicHandler MagicHandler {
            get { return magic_handler; }
        }

        public string FormatName {
            get { return name; }
        }

        public string FileExtension {
            get { return extension; }
        }

        public string [] MimeTypes {
            get { return mimetypes; }
        }
    }
}
