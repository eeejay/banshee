//
// Sort.cs
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

using Mono.Unix;

namespace InternetArchive
{
    /*
    "" = relevance
    addeddate desc
    addeddate asc
    avg_rating desc
    avg_rating asc
    call_number desc

    call_number asc
    createdate desc
    createdate asc
    creatorSorter desc
    creatorSorter asc
    date desc
    date asc
    downloads desc
    downloads asc

    foldoutcount desc
    foldoutcount asc
    headerImage desc
    headerImage asc
    identifier desc
    identifier asc
    imagecount desc
    imagecount asc
    indexdate desc

    indexdate asc
    languageSorter desc
    languageSorter asc
    licenseurl desc
    licenseurl asc
    month desc
    month asc
    nav_order desc
    nav_order asc

    num_reviews desc
    num_reviews asc
    publicdate desc
    publicdate asc
    reviewdate desc
    reviewdate asc
    stars desc
    stars asc
    titleSorter desc

    titleSorter asc
    updatedate desc
    updatedate asc
    week desc
    week asc
    year desc
    year asc */

    public class Sort
    {
        public static Sort DownloadsDesc  = new Sort ("downloads desc",  Catalog.GetString ("Downloads"));
        public static Sort WeekDesc       = new Sort ("week desc",       Catalog.GetString ("Downloads This Week"));
        public static Sort DateCreatedDesc= new Sort ("createdate desc", Catalog.GetString ("Newest"));
        public static Sort DateCreatedAsc = new Sort ("createdate asc",  Catalog.GetString ("Oldest"));
        public static Sort DateAddedDesc  = new Sort ("addeddate desc",  Catalog.GetString ("Recently Added"));
        public static Sort AvgRatingDesc  = new Sort ("avg_rating desc", Catalog.GetString ("Rating"));
        public static Sort TitleAsc       = new Sort ("titleSorter asc", Catalog.GetString ("Title"));
        public static Sort CreatorAsc     = new Sort ("creatorSorter asc", Catalog.GetString ("Creator"));

        public Sort () {}

        public Sort (string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
    }
}
