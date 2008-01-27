/***************************************************************************
 *  PodcastPixbufs.cs
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

using Gdk;
using Gtk;

using Banshee.Base;

namespace Banshee.Plugins.Podcast.UI
{
    // These should all be registered with the StockManager.

    public static class PodcastPixbufs
    {    
        private static Pixbuf podcast_icon_16;
        private static Pixbuf podcast_icon_22;
        private static Pixbuf podcast_icon_48;
     
        private static Pixbuf new_podcast_icon;        
        
        private static Pixbuf download_column_icon;
        private static Pixbuf activity_column_icon;

        static PodcastPixbufs ()
        {
            new_podcast_icon = Gdk.Pixbuf.LoadFromResource ("banshee-new.png");            
        
            podcast_icon_16 = Gdk.Pixbuf.LoadFromResource ("podcast-icon-16.png");
            podcast_icon_22 = Gdk.Pixbuf.LoadFromResource ("podcast-icon-22.png");
            podcast_icon_48 = Gdk.Pixbuf.LoadFromResource ("podcast-icon-48.png");
            
            activity_column_icon = IconThemeUtils.LoadIcon (16, "audio-volume-high", "blue-speaker");
            download_column_icon = Gdk.Pixbuf.LoadFromResource("document-save-as-16.png");
        }

        public static Pixbuf NewPodcastIcon {
            get
            {
                return new_podcast_icon;
            }
        }

        public static Pixbuf ActivityColumnIcon {
            get
            {
                return activity_column_icon;
            }
        }

        public static Pixbuf DownloadColumnIcon {
            get
            {
                return download_column_icon;
            }
        }
        
        public static Pixbuf PodcastIcon16 {
            get
            {
                return podcast_icon_16;
            }
        }

        public static Pixbuf PodcastIcon22 {
            get
            {
                return podcast_icon_22;
            }
        }

        public static Pixbuf PodcastIcon48 {
            get
            {
                return podcast_icon_48;
            }
        }
    }
}
