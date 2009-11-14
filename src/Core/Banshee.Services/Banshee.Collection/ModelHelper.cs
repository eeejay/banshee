//
// ModelHelper.cs
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
using System.Collections.Generic;

namespace Banshee.Collection
{
    public static class ModelHelper
    {
        public delegate string SingleIdFilterHandler<T>(T t);
        public delegate void IdFilterChangedHandler(string newFilter);

        public static void BuildIdFilter<T>(IEnumerable<T> value, string field, string oldFilter,
            SingleIdFilterHandler<T> singleFilterHandler, IdFilterChangedHandler filterChangedHandler)
        {
            int count = 0;
            string new_filter = null;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            if(value != null) {
                foreach(T t in value) {
                    string t_f = singleFilterHandler(t);
                    if(t_f != null) {
                        builder.AppendFormat("{0},", t_f);
                        count++;
                    }
                }

                if(count > 0) {
                    builder.Remove(builder.Length - 1, 1);
                    builder.Insert(0, String.Format(" {0} IN (", field));
                    builder.Append(") ");

                    new_filter = builder.ToString();
                }
            }

            if(new_filter != oldFilter) {
                filterChangedHandler(new_filter);
            }
        }
    }
}
