//
// BansheeQueryBox.cs
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

using Mono.Unix;

using Hyena.Query;
using Hyena.Query.Gui;

using Banshee.Query;

namespace Banshee.Query.Gui
{
    public class BansheeQueryBox : QueryBox
    {
        public BansheeQueryBox () : base (BansheeQuery.FieldSet)
        {
        }

        static BansheeQueryBox () {
            // Register our custom query value entries
            QueryValueEntry.AddSubType (typeof(RatingQueryValueEntry), typeof(RatingQueryValue));
            QueryValueEntry.AddSubType (typeof(PlaylistQueryValueEntry), typeof(PlaylistQueryValue));

            // Set translated names for operators
            IntegerQueryValue.Equal.Label            = Catalog.GetString ("is");
            IntegerQueryValue.NotEqual.Label         = Catalog.GetString ("is not");
            IntegerQueryValue.LessThanEqual.Label    = Catalog.GetString ("less than");
            IntegerQueryValue.GreaterThanEqual.Label = Catalog.GetString ("more than");
            IntegerQueryValue.LessThan.Label         = Catalog.GetString ("at most");
            IntegerQueryValue.GreaterThan.Label      = Catalog.GetString ("at least");

            DateQueryValue.Equal.Label               = Catalog.GetString ("is");
            DateQueryValue.NotEqual.Label            = Catalog.GetString ("is not");
            DateQueryValue.LessThanEqual.Label       = Catalog.GetString ("less than");
            DateQueryValue.GreaterThanEqual.Label    = Catalog.GetString ("more than");
            DateQueryValue.LessThan.Label            = Catalog.GetString ("at most");
            DateQueryValue.GreaterThan.Label         = Catalog.GetString ("at least");

            StringQueryValue.Equal.Label             = Catalog.GetString ("is");
            StringQueryValue.NotEqual.Label          = Catalog.GetString ("is not");
            StringQueryValue.Contains.Label          = Catalog.GetString ("contains");
            StringQueryValue.DoesNotContain.Label    = Catalog.GetString ("doesn't contain");
            StringQueryValue.StartsWith.Label        = Catalog.GetString ("starts with");
            StringQueryValue.EndsWith.Label          = Catalog.GetString ("ends with");
        }
    }
}
