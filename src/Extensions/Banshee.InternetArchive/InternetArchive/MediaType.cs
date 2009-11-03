//
// MediaType.cs
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

namespace InternetArchive
{
    public class MediaType : FieldValue
    {
        public MediaType (string id, string name) : base (Field.MediaType, id, name)
        {
            Children = new List<Collection> ();
        }

        public MediaType AddChildren (params Collection [] children)
        {
            foreach (var child in children) {
                Children.Add (child);
                child.MediaType = this;
            }

            return this;
        }

        public IList<Collection> Children { get; set; }

        public static FieldValue Get (string id)
        {
            foreach (var type in Options) {
                if (type.Id == id) {
                    return type;
                }

                foreach (var collection in type.Children) {
                    if (collection.Id == id) {
                        return collection;
                    }
                }
            }

            return null;
        }

        public static MediaType [] Options = new MediaType [] {
            new MediaType ("movies", Catalog.GetString ("Moving Images")).AddChildren (
                new Collection ("animationandcartoons", Catalog.GetString ("Animation & Cartoons")),
                new Collection ("artsandmusicvideos", Catalog.GetString ("Arts & Music")),
                new Collection ("computersandtechvideos", Catalog.GetString ("Computers & Technology")),
                new Collection ("culturalandacademicfilms", Catalog.GetString ("Cultural & Academic Films")),
                new Collection ("ephemera", Catalog.GetString ("Ephemeral Films")),
                new Collection ("home_movies", Catalog.GetString ("Home Movies")),
                new Collection ("moviesandfilms", Catalog.GetString ("Movies")),
                new Collection ("newsandpublicaffairs", Catalog.GetString ("News & Public Affairs")),
                new Collection ("opensource_movies", Catalog.GetString ("Open Source Movies")),
                new Collection ("prelinger", Catalog.GetString ("Prelinger Archives")),
                new Collection ("spiritualityandreligion", Catalog.GetString ("Spirituality & Religion")),
                new Collection ("sports", Catalog.GetString ("Sports Videos")),
                new Collection ("gamevideos", Catalog.GetString ("Videogame Videos")),
                new Collection ("vlogs", Catalog.GetString ("Vlogs")),
                new Collection ("youth_media", Catalog.GetString ("Youth Media"))
            ),
            new MediaType ("texts", Catalog.GetString ("Texts")).AddChildren (
                new Collection ("americana", Catalog.GetString ("American Libraries")),
                new Collection ("toronto", Catalog.GetString ("Canadian Libraries")),
                new Collection ("universallibrary", Catalog.GetString ("Universal Library")),
                new Collection ("gutenberg", Catalog.GetString ("Project Gutenberg")),
                new Collection ("iacl", Catalog.GetString ("Children's Library")),
                new Collection ("biodiversity", Catalog.GetString ("Biodiversity Heritage Library")),
                new Collection ("additional_collections", Catalog.GetString ("Additional Collections"))
            ),
            new MediaType ("audio", Catalog.GetString ("Audio")).AddChildren (
                new Collection ("audio_bookspoetry", Catalog.GetString ("Audio Books & Poetry")),
                new Collection ("audio_tech", Catalog.GetString ("Computers & Technology")),
                new Collection ("GratefulDead", Catalog.GetString ("Grateful Dead")),
                new Collection ("etree", Catalog.GetString ("Live Music Archive")),
                new Collection ("audio_music", Catalog.GetString ("Music & Arts")),
                new Collection ("netlabels", Catalog.GetString ("Netlabels")),
                new Collection ("audio_news", Catalog.GetString ("News & Public Affairs")),
                new Collection ("audio_foreign", Catalog.GetString ("Non-English Audio")),
                new Collection ("opensource_audio", Catalog.GetString ("Open Source Audio")),
                new Collection ("audio_podcast", Catalog.GetString ("Podcasts")),
                new Collection ("radioprograms", Catalog.GetString ("Radio Programs")),
                new Collection ("audio_religion", Catalog.GetString ("Spirituality & Religion"))
            ),
            new MediaType ("education", Catalog.GetString ("Education")),
            new MediaType ("software", Catalog.GetString ("Software")).AddChildren (
                new Collection ("clasp", Catalog.GetString ("CLASP"))
            )
        };
    }
}
