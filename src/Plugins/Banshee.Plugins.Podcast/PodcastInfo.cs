/***************************************************************************
 *  PodcastInfo.cs
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
using System.IO;

using Gtk;
using Banshee.Base;
using Banshee.Configuration.Schema;

using Banshee.Plugins.Podcast.Download;

namespace Banshee.Plugins.Podcast
{
    public class PodcastInfo : IComparable
    {
        private int id;
        private PodcastFeedInfo feed;

        // I really don't like making this code dependent on Gtk.
        private TreeIter treeIter;

        private string title;
        private string link;
        private string description;
        private string local_path;

        private SafeUri url;
        private long length;
        private string author;
        private string mime_type;
        private DateTime pub_date;

        private bool queued;
        private bool active;
        private bool failed;
        private bool downloaded;
        private bool downloading;

        private TrackInfo track = null;
        private DownloadInfo dif = null;

        private readonly object sync_root = new object ();

        public object SyncRoot {
            get
            {
                return sync_root;
            }
        }

        public int CompareTo (object o)
        {
            if (!(o is PodcastInfo))
            {
                return 0;
            }

            PodcastInfo rhs = o as PodcastInfo;

            return DateTime.Compare (rhs.pub_date, this.pub_date);
        }

        public PodcastInfo (PodcastFeedInfo feed, string url) : this (0, feed, url)
        {
            active = true;
            downloaded = false;
        }

        private PodcastInfo (int id, PodcastFeedInfo feed, string url)
        {
            treeIter = TreeIter.Zero;

            this.id = id;
            this.feed = feed;
            this.url = new SafeUri (url);
        }

        // Fix constructors.  Need a better way of creating new podcasts.
        public PodcastInfo (PodcastFeedInfo feed, int id, string title, string link, DateTime pubDate,
                            string description, string author, string local_path, string url, string mime_type, long length,
                            bool downloaded, bool active) : this (id, feed, url)
        {
            this.title = title;
            this.description = description;
            this.pub_date = pubDate;
            this.link = link;
            this.author = author;
            this.active = active;
            this.downloaded = downloaded;
            this.mime_type = mime_type;
            this.length = length;
            this.local_path = local_path;
            this.track = null;

            if (active && downloaded)
            {
                if (local_path != null && local_path != String.Empty)
                {
                    try
                    {
                        track = (TrackInfo)
                                Globals.Library.TracksFnKeyed[
                                    Banshee.Base.Library.MakeFilenameKey (new SafeUri (local_path))
                                ];
                    }
                    catch {}
                }
        }
    }

    public int ID {
        get
        { return id; }
        internal set
        { id = value; }
    }

    public bool IsActive {
        get
        { return active; }
        internal set
        { active = value; }
    }

    public bool IsDownloaded {
        get
        { return downloaded; }
        internal set
        { downloaded = value; }
    }

    public string Title {
        get
        {return title;}
        internal set
        { title = value; }
    }

    public PodcastFeedInfo Feed {
        get
        {return feed;}
    }

    public string Description {
        get
        { return description; }
        internal set
        { description = value; }
    }

    public string Author {
        get
        { return author; }
        internal set
        { author = value; }
    }

    public DateTime PubDate {
        get
        { return pub_date; }
        internal set
        { pub_date = value; }
    }

    public SafeUri Url {
        get
        { return url; }
        internal set
        { url = value; }
    }

    public string LocalPath {
        get
        {
            return local_path;
        }

        internal set
        {
            local_path = value;
        }
    }

    public string LocalDirectoryPath {

        // --TODO This ugly, fix it, put this stuff in a paths directory or something.
        //   Also, make sure you add a config option
        get
        {
            if (url != null && feed != null)
                {
                    return LibrarySchema.Location.Get () +
                            Path.DirectorySeparatorChar + "Podcasts" +
                            Path.DirectorySeparatorChar + SanitizeName (feed.Title) +
                            Path.DirectorySeparatorChar;
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        public string MimeType {
            get
            { return mime_type; }
            internal set
            { mime_type = value; }
        }

        // TODO use System.Uri
        public string Link {
            get
            { return link; }
            internal set
            { link = value; }
        }

        public long Length {
            get
            { return length; }
            internal set
            { length = value; }
        }

        public TrackInfo Track {
            get
            { return track; }
            internal set
            { track = value; }
        }

        public TreeIter TreeIter {
            get
            { return treeIter; }
            internal set
            { treeIter = value; }
        }

        public bool CanDownload {
            get
            {
                return (active & !downloaded & !queued);
            }
        }

        public bool CanCancel {
            get
            {
                return (active & !downloaded & queued);
            }
        }

        public bool IsQueued {
            get
            { return queued; }
            internal set
            {
                if (queued != value)
                {
                    if (value)
                    {
                        feed.IncrementQueuedDownloads ();
                    }
                    else
                    {
                        feed.DecrementQueuedDownloads ();
                    }
                    queued = value;
                }
            }
        }

        public bool IsDownloading {
            get
            { return downloading; }
            internal set
            {
                if (downloading != value)
                {
                    if (value)
                    {
                        feed.IncrementActiveDownloads ();
                    }
                    else
                    {
                        feed.DecrementActiveDownloads ();
                    }
                    downloading = value;
                }
            }
        }

        public bool DownloadFailed {
            get
            { return failed; }
            internal set
            { failed = value; }
        }

        public DownloadInfo DownloadInfo {
            get
            { return dif; }
            internal set
            { dif = value; }
        }

        public string Key {
            get
            {
                return url.ToString ();
            }
        }

        // Via Monopod
        private static string SanitizeName (string s)
        {
            // remove /, : and \ from names
            return s.Replace ('/', '_').Replace ('\\', '_').Replace (':', '_');
        }
    }
}
