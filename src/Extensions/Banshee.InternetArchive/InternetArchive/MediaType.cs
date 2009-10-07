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
        private MediaType (string id, string name) : base (Field.MediaType, id, name) {}

        private MediaType AddChildren (params FieldValue [] children)
        {
            if (Children == null) {
                Children = new List<FieldValue> ();
            }

            foreach (var child in children) {
                Children.Add (child);
            }

            return this;
        }

        public IList<FieldValue> Children { get; set; }

        public static MediaType [] Options = new MediaType [] {
            new MediaType ("movies", Catalog.GetString ("Moving Images")).AddChildren (
                new FieldValue (Field.Collection, "animationandcartoons", Catalog.GetString ("Animation & Cartoons")),
                new FieldValue (Field.Collection, "artsandmusicvideos", Catalog.GetString ("Arts & Music")),
                new FieldValue (Field.Collection, "computersandtechvideos", Catalog.GetString ("Computers & Technology")),
                new FieldValue (Field.Collection, "culturalandacademicfilms", Catalog.GetString ("Cultural & Academic Films")),
                new FieldValue (Field.Collection, "ephemera", Catalog.GetString ("Ephemeral Films")),
                new FieldValue (Field.Collection, "home_movies", Catalog.GetString ("Home Movies")),
                new FieldValue (Field.Collection, "moviesandfilms", Catalog.GetString ("Movies")),
                new FieldValue (Field.Collection, "newsandpublicaffairs", Catalog.GetString ("News & Public Affairs")),
                new FieldValue (Field.Collection, "opensource_movies", Catalog.GetString ("Open Source Movies")),
                new FieldValue (Field.Collection, "prelinger", Catalog.GetString ("Prelinger Archives")),
                new FieldValue (Field.Collection, "spiritualityandreligion", Catalog.GetString ("Spirituality & Religion")),
                new FieldValue (Field.Collection, "sports", Catalog.GetString ("Sports Videos")),
                new FieldValue (Field.Collection, "gamevideos", Catalog.GetString ("Videogame Videos")),
                new FieldValue (Field.Collection, "vlogs", Catalog.GetString ("Vlogs")),
                new FieldValue (Field.Collection, "youth_media", Catalog.GetString ("Youth Media"))
            ),
            new MediaType ("texts", Catalog.GetString ("Texts")).AddChildren (
                new FieldValue (Field.Collection, "americana", Catalog.GetString ("American Libraries")),
                new FieldValue (Field.Collection, "toronto", Catalog.GetString ("Canadian Libraries")),
                new FieldValue (Field.Collection, "universallibrary", Catalog.GetString ("Universal Library")),
                new FieldValue (Field.Collection, "gutenberg", Catalog.GetString ("Project Gutenberg")),
                new FieldValue (Field.Collection, "iacl", Catalog.GetString ("Children's Library")),
                new FieldValue (Field.Collection, "biodiversity", Catalog.GetString ("Biodiversity Heritage Library")),
                new FieldValue (Field.Collection, "additional_collections", Catalog.GetString ("Additional Collections"))
            ),
            new MediaType ("audio", Catalog.GetString ("Audio")).AddChildren (
                new FieldValue (Field.Collection, "audio_bookspoetry", Catalog.GetString ("Audio Books & Poetry")),
                new FieldValue (Field.Collection, "audio_tech", Catalog.GetString ("Computers & Technology")),
                new FieldValue (Field.Collection, "GratefulDead", Catalog.GetString ("Grateful Dead")),
                new FieldValue (Field.Collection, "etree", Catalog.GetString ("Live Music Archive")),
                new FieldValue (Field.Collection, "audio_music", Catalog.GetString ("Music & Arts")),
                new FieldValue (Field.Collection, "netlabels", Catalog.GetString ("Netlabels")),
                new FieldValue (Field.Collection, "audio_news", Catalog.GetString ("News & Public Affairs")),
                new FieldValue (Field.Collection, "audio_foreign", Catalog.GetString ("Non-English Audio")),
                new FieldValue (Field.Collection, "opensource_audio", Catalog.GetString ("Open Source Audio")),
                new FieldValue (Field.Collection, "audio_podcast", Catalog.GetString ("Podcasts")),
                new FieldValue (Field.Collection, "radioprograms", Catalog.GetString ("Radio Programs")),
                new FieldValue (Field.Collection, "audio_religion", Catalog.GetString ("Spirituality & Religion"))
            ),
            new MediaType ("education", Catalog.GetString ("Education")),
            new MediaType ("software", Catalog.GetString ("Software")).AddChildren (
                new FieldValue (Field.Collection, "clasp", Catalog.GetString ("CLASP"))
            )
        };
    }
}
