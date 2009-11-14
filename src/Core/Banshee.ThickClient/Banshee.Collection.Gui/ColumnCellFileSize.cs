//
// ColumnCellFileSize.cs
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

using Hyena.Query;
using Hyena.Data.Gui;

namespace Banshee.Collection.Gui
{
    public class ColumnCellFileSize : ColumnCellText
    {
        public ColumnCellFileSize (string property, bool expand) : base (property, expand)
        {
            Alignment = Pango.Alignment.Right;
            SetMinMaxStrings ((long)1023, (long)(1024 * 1024 * 575.5));
        }

        protected override string GetText (object obj)
        {
            if (obj == null) {
                return String.Empty;
            }

            long bytes = (long) obj;
            return bytes <= 0
                ? String.Empty
                : new FileSizeQueryValue (bytes).ToUserQuery (true);
        }
    }
}
