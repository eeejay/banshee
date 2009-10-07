// 
// ColumnCellPositiveInt.cs
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

using Hyena.Data.Gui;

namespace Banshee.Collection.Gui
{
    public class ColumnCellPositiveInt : ColumnCellText
    {
        public bool CultureFormatted { get; set; }

        public ColumnCellPositiveInt (string property, bool expand, int min_digits, int max_digits) : base (property, expand)
        {
            SetMinMaxStrings ((int)Math.Pow (10, min_digits) - 1, (int)Math.Pow (10, max_digits) - 1);
            Alignment = Pango.Alignment.Right;
            CultureFormatted = true;
        }
        
        protected override string GetText (object obj)
        {
            if (obj == null) {
                return String.Empty;
            }

            int val = (int) obj;

            if (val <= 0)
                return "";
            else if (CultureFormatted)
                return val.ToString ("N0");
            else
                return val.ToString ();
        }
    }
}
