//
// GstErrors.cs
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

namespace Banshee.GStreamer
{
    internal enum GstCoreError
    {
        Failed = 1,
        TooLazy,
        NotImplemented,
        StateChange,
        Pad,
        Thread,
        Negotiation,
        Event,
        Seek,
        Caps,
        Tag,
        MissingPlugin,
        Clock,
        NumErrors
    }

    internal enum GstLibraryError
    {
        Failed = 1,
        Init,
        Shutdown,
        Settings,
        Encode,
        NumErrors
    }

    internal enum GstResourceError
    {
        Failed = 1,
        TooLazy,
        NotFound,
        Busy,
        OpenRead,
        OpenWrite,
        OpenReadWrite,
        Close,
        Read,
        Write,
        Seek,
        Sync,
        Settings,
        NoSpaceLeft,
        NumErrors
    }

    internal enum GstStreamError
    {
        Failed = 1,
        TooLazy,
        NotImplemented,
        TypeNotFound,
        WrongType,
        CodecNotFound,
        Decode,
        Encode,
        Demux,
        Mux,
        Format,
        NumErrors
    }
}
