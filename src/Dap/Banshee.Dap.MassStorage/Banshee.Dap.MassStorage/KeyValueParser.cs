//
// KeyValueParser.cs
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
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Banshee.Dap.MassStorage
{
    internal class KeyValueParser : Dictionary<string, string []>
    {
        private StreamReader reader;
        private StringBuilder builder = new StringBuilder (64);
        private List<string> values = new List<string> (8);
        private string key;

        public KeyValueParser (StreamReader reader)
        {
            this.reader = reader;
            Parse ();
        }

        private void Parse ()
        {
            bool in_value = false;
            char c;

            while ((c = Read ()) != Char.MaxValue) {
                if (!in_value && c == '=') {
                    key = Collect ();
                    if (!String.IsNullOrEmpty (key)) {
                        in_value = true;
                    } else {
                        key = null;
                    }
                } else if (Char.IsWhiteSpace (c) || c == ',') {
                    if (!in_value) {
                        continue;
                    }

                    string value = Collect ();
                    if (!String.IsNullOrEmpty (value)) {
                        values.Add (value);
                    }

                    if (c == '\n') {
                        Commit ();
                        in_value = false;
                    }
                } else {
                    if (c == '\\') {
                        c = Read ();
                    }

                    if (c != '\n') {
                        builder.Append (c);
                    }
                }
            }

            Commit ();
        }

        private void Commit ()
        {
            if (!String.IsNullOrEmpty (key) && values.Count > 0) {
                if (ContainsKey (key)) {
                    this[key] = values.ToArray ();
                } else {
                    Add (key, values.ToArray ());
                }
            }

            key = null;
            values.Clear ();
        }

        private string Collect ()
        {
            if (builder.Length == 0) {
                return null;
            }

            string value = builder.ToString ();
            builder.Remove (0, builder.Length);
            return value;
        }

        private char Read ()
        {
            return (char)reader.Read ();
        }
    }
}
