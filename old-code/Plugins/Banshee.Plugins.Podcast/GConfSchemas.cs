/***************************************************************************
 *  PodcastGConfKeys.cs
 *
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;

using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.Plugins.Podcast
{
    public static class GConfSchemas
    {
        private const string gconfNamespace = "plugins.podcast";
        private const string UI = gconfNamespace + ".ui";
        private const string Columns = UI + ".columns";
        
        public static readonly SchemaEntry<string> PodcastLibrarySchema 
            = new SchemaEntry<string> (
                gconfNamespace, "podcast_library", 
                String.Empty, "Podcast library location",
                "Root directory for the podcast plugin to store downloaded files"
            );

        public static readonly SchemaEntry<int> PlaylistSeparatorPositionSchema 
            = new SchemaEntry<int> (
                UI, "playlist_separator_position", 
                300, "Playlist separator position",
                "Position of the separator located between the feed and podcast views"
            );

        public static readonly SchemaEntry<int> PodcastTitleColumnSchema 
            = new SchemaEntry<int> (
                Columns, "podcast_title_column", 
                100, "Podcast title column",
                "Position of the podcast title playlist column"
            );

        public static readonly SchemaEntry<int> PodcastFeedColumnSchema 
            = new SchemaEntry<int> (
                Columns, "podcast_feed_column", 
                100, "Podcast feed column",
                "Position of the podcast feed playlist column"
            );

        public static readonly SchemaEntry<int> PodcastDateColumnSchema 
            = new SchemaEntry<int> (
                Columns, "podcast_date_column", 
                100, "Podcast date column",
                "Position of the podcast date playlist column"
            );            
    }
}
