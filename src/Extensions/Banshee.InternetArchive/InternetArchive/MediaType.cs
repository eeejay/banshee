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

        private MediaType AddChildren (params MediaType [] children)
        {
            if (Children == null) {
                Children = new List<MediaType> ();
            }

            foreach (var child in children) {
                Children.Add (child);
            }

            return this;
        }

        public IList<MediaType> Children { get; set; }

        public static MediaType [] Options = new MediaType [] {
            new MediaType ("movies", Catalog.GetString ("Moving Images")).AddChildren (
                new MediaType ("movies.animationandcartoons", Catalog.GetString ("Animation & Cartoons")),
                new MediaType ("movies.artsandmusicvideos", Catalog.GetString ("Arts & Music")),
                new MediaType ("movies.computersandtechvideos", Catalog.GetString ("Computers & Technology")),
                new MediaType ("movies.culturalandacademicfilms", Catalog.GetString ("Cultural & Academic Films")),
                new MediaType ("movies.ephemera", Catalog.GetString ("Ephemeral Films")),
                new MediaType ("movies.home_movies", Catalog.GetString ("Home Movies")),
                new MediaType ("movies.moviesandfilms", Catalog.GetString ("Movies")),
                new MediaType ("movies.newsandpublicaffairs", Catalog.GetString ("News & Public Affairs")),
                new MediaType ("movies.opensource_movies", Catalog.GetString ("Open Source Movies")),
                new MediaType ("movies.prelinger", Catalog.GetString ("Prelinger Archives")),
                new MediaType ("movies.spiritualityandreligion", Catalog.GetString ("Spirituality & Religion")),
                new MediaType ("movies.sports", Catalog.GetString ("Sports Videos")),
                new MediaType ("movies.gamevideos", Catalog.GetString ("Videogame Videos")),
                new MediaType ("movies.vlogs", Catalog.GetString ("Vlogs")),
                new MediaType ("movies.youth_media", Catalog.GetString ("Youth Media"))
            ),
            new MediaType ("texts", Catalog.GetString ("Texts")).AddChildren (
                new MediaType ("texts.americana", Catalog.GetString ("American Libraries")),
                new MediaType ("texts.toronto", Catalog.GetString ("Canadian Libraries")),
                new MediaType ("texts.universallibrary", Catalog.GetString ("Universal Library")),
                new MediaType ("texts.gutenberg", Catalog.GetString ("Project Gutenberg")),
                new MediaType ("texts.iacl", Catalog.GetString ("Children's Library")),
                new MediaType ("texts.biodiversity", Catalog.GetString ("Biodiversity Heritage Library")),
                new MediaType ("texts.additional_collections", Catalog.GetString ("Additional Collections"))
            ),
            new MediaType ("audio", Catalog.GetString ("Audio")).AddChildren (
                new MediaType ("audio.audio_bookspoetry", Catalog.GetString ("Audio Books & Poetry")),
                new MediaType ("audio.audio_tech", Catalog.GetString ("Computers & Technology")),
                new MediaType ("audio.GratefulDead", Catalog.GetString ("Grateful Dead")),
                new MediaType ("audio.etree", Catalog.GetString ("Live Music Archive")),
                new MediaType ("audio.audio_music", Catalog.GetString ("Music & Arts")),
                new MediaType ("audio.netlabels", Catalog.GetString ("Netlabels")),
                new MediaType ("audio.audio_news", Catalog.GetString ("News & Public Affairs")),
                new MediaType ("audio.audio_foreign", Catalog.GetString ("Non-English Audio")),
                new MediaType ("audio.opensource_audio", Catalog.GetString ("Open Source Audio")),
                new MediaType ("audio.audio_podcast", Catalog.GetString ("Podcasts")),
                new MediaType ("audio.radioprograms", Catalog.GetString ("Radio Programs")),
                new MediaType ("audio.audio_religion", Catalog.GetString ("Spirituality & Religion"))
            ),
            new MediaType ("education", Catalog.GetString ("Education")),
            new MediaType ("software", Catalog.GetString ("Software")).AddChildren (
                new MediaType ("software.clasp", Catalog.GetString ("CLASP"))
            )
        };
    }
}
