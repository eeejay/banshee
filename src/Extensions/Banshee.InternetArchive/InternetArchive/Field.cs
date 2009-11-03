//
// Field.cs
//
// Author:
//       Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2009 Gabriel Burt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Query;

namespace InternetArchive
{
    public class Field
    {
        /*avg_rating
        call_number
        coverage

        foldoutcount
        headerImage
        imagecount

        month
        oai_updatedate
        publicdate
        rights

        scanningcentre
        source
        subject
        type
        volume
        week
        */

        private static List<Field> fields = new List<Field> ();
        public static Field AvgRating  = new Field ("avg_rating", Catalog.GetString ("Rating"));
        public static Field Creator    = new Field ("creator",    Catalog.GetString ("Creator"));
        public static Field Collection = new Field ("collection", Catalog.GetString ("Collection"));
        public static Field Contributor= new Field ("contributor",Catalog.GetString ("Contributor"));
        public static Field Date       = new Field ("date",       Catalog.GetString ("Created"));
        public static Field DateAdded  = new Field ("publicdate", Catalog.GetString ("Added"));
        public static Field Description= new Field ("description",Catalog.GetString ("Description"));
        public static Field Downloads  = new Field ("downloads",  Catalog.GetString ("Downloads"));
        public static Field Format     = new Field ("format",     Catalog.GetString ("Format"));
        public static Field Identifier = new Field ("identifier", Catalog.GetString ("ID"));
        public static Field Language   = new Field ("language",   Catalog.GetString ("Language"));
        public static Field LicenseUrl = new Field ("licenseurl", Catalog.GetString ("License"));
        public static Field License    = new Field ("license",    Catalog.GetString ("License"));
        public static Field MediaType  = new Field ("mediatype",  Catalog.GetString ("Media Type"));
        public static Field NumReviews = new Field ("num_reviews",Catalog.GetString ("Review Count"));
        public static Field Publisher  = new Field ("publisher",  Catalog.GetString ("Publisher"));
        public static Field Title      = new Field ("title",      Catalog.GetString ("Title"));
        public static Field Year       = new Field ("year",       Catalog.GetString ("Year"));

        public static IEnumerable<Field> Fields {
            get { return fields; }
        }

        public string Name { get; private set; }
        public string Id { get; private set; }

        public Field (string id, string name)
        {
            Id = id;
            Name = name;

            fields.Add (this);
            fields.Sort ((a, b) => a.Name.CompareTo (b.Name));
        }
    }
}
