/***************************************************************************
 *  PodcastPlaylistView.cs
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

using Gtk;
using Gdk;
using Pango;

using Mono.Gettext;

using System;
using System.Collections;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Configuration;

using Banshee.Plugins.Podcast;
using Banshee.Plugins.Podcast.Download;

namespace Banshee.Plugins.Podcast.UI
{
    // Stop-gap solution.  Will talk to Oscar about unifying
    // Playlist code.
    internal class PodcastPlaylistView : TreeView, IDisposable
    {
        private enum Column : int 
        {
            Activity = 0,
            Download = 1,
            PodcastTitle = 2,
            FeedTitle = 3,
            PubDate = 4
        }

        private TreeModelSort sort;
        private TreeModelFilter filter;

        private ArrayList columns;
        private ArrayList schemas;

        private TreeViewColumn activity_column;
        private TreeViewColumn download_column;
        private TreeViewColumn podcast_title_column;
        private TreeViewColumn feed_title_column;
        private TreeViewColumn pubdate_column;

        private PodcastFeedInfo selected_feed = PodcastFeedInfo.All;

        public PodcastPlaylistView (PodcastPlaylistModel model)
        {
            if (model == null)
            {
                throw new NullReferenceException ("model");
            }

            columns = new ArrayList (3);
            schemas = new ArrayList (3);
            
            RulesHint = true;
            Selection.Mode = SelectionMode.Multiple;

            filter = new TreeModelFilter (model, null);
            filter.VisibleFunc = PodcastFilterFunc;

            sort = new TreeModelSort (filter);
            sort.DefaultSortFunc = PodcastFeedTitleTreeIterCompareFunc;

            Model = sort;

            podcast_title_column = NewColumn (
                Catalog.GetString ("Episode"), 
                (int) Column.PodcastTitle,
                GConfSchemas.PodcastTitleColumnSchema
            );
            
            feed_title_column = NewColumn (
                Catalog.GetString ("Podcast"), 
                (int) Column.FeedTitle,
                GConfSchemas.PodcastFeedColumnSchema
            );
            
            pubdate_column = NewColumn (
                Catalog.GetString ("Date"), 
                (int) Column.PubDate,
                GConfSchemas.PodcastDateColumnSchema
            );
            
            pubdate_column.Resizable = false;
            pubdate_column.FixedWidth = 120;

            /********************************************/

            download_column = new TreeViewColumn();

            Gtk.Image download_image = new Gtk.Image(
                PodcastPixbufs.DownloadColumnIcon
            );

            download_image.Show();

            download_column.Expand = false;
            download_column.Resizable = false;
            download_column.Clickable = false;
            download_column.Reorderable = false;
            download_column.Visible = true;
            download_column.Widget = download_image;

            CellRendererToggle download_renderer = new CellRendererToggle();

            download_renderer.Activatable = true;
            download_renderer.Toggled += OnDownloadToggled;
            download_column.PackStart(download_renderer, true);

            download_column.SetCellDataFunc (
                download_renderer, new TreeCellDataFunc(DownloadCellToggle));

            /********************************************/
            activity_column = new TreeViewColumn();

            Gtk.Image activity_image = new Gtk.Image(
                PodcastPixbufs.ActivityColumnIcon
            );

            activity_image.Show();

            activity_column.Expand = false;
            activity_column.Resizable = false;
            activity_column.Clickable = false;
            activity_column.Reorderable = false;
            activity_column.Visible = true;
            activity_column.Widget = activity_image;

            CellRendererPixbuf activity_renderer = new CellRendererPixbuf();
            activity_column.PackStart(activity_renderer, true);

            activity_column.SetCellDataFunc (activity_renderer,
                                             new TreeCellDataFunc (TrackCellActivity));

            /********************************************/

            CellRendererText podcast_title_renderer = new CellRendererText();
            CellRendererText feed_title_renderer = new CellRendererText();
            CellRendererText pubdate_renderer = new CellRendererText();

            podcast_title_column.PackStart (podcast_title_renderer, false);
            podcast_title_column.SetCellDataFunc (podcast_title_renderer,
                                                  new TreeCellDataFunc (TrackCellPodcastTitle));

            feed_title_column.PackStart (feed_title_renderer, true);
            feed_title_column.SetCellDataFunc (feed_title_renderer,
                                               new TreeCellDataFunc (TrackCellPodcastFeedTitle));

            pubdate_column.PackStart (pubdate_renderer, true);
            pubdate_column.SetCellDataFunc (pubdate_renderer,
                                            new TreeCellDataFunc (TrackCellPubDate));

            sort.SetSortFunc((int)Column.PodcastTitle,
                             new TreeIterCompareFunc(PodcastTitleTreeIterCompareFunc));
            sort.SetSortFunc((int)Column.FeedTitle,
                             new TreeIterCompareFunc(PodcastFeedTitleTreeIterCompareFunc));
            sort.SetSortFunc((int)Column.PubDate,
                             new TreeIterCompareFunc(PodcastPubDateTreeIterCompareFunc));

            InsertColumn (activity_column, (int)Column.Activity);
            InsertColumn (download_column, (int)Column.Download);
            InsertColumn (podcast_title_column, (int)Column.PodcastTitle);
            InsertColumn (feed_title_column, (int)Column.FeedTitle);
            InsertColumn (pubdate_column, (int)Column.PubDate);
        }

        private TreeViewColumn NewColumn (string title, 
                                          int sortColumn, 
                                          SchemaEntry<int> schema)
        {
            TreeViewColumn tmp_column = new TreeViewColumn ();

            tmp_column.Resizable = true;
            tmp_column.Clickable = true;
            tmp_column.Reorderable = false;
            tmp_column.Visible = true;
            tmp_column.Sizing = TreeViewColumnSizing.Fixed;
            tmp_column.Title = title;
            tmp_column.SortColumnId = sortColumn;

            tmp_column.FixedWidth = schema.Get ();

            schemas.Add (schema);
            columns.Add (tmp_column);
            
            return tmp_column;
        }

        public void Shutdown ()
        {
            for (int i = 0; i < columns.Count; ++i) {
                TreeViewColumn tmpCol = columns[i] as TreeViewColumn;
                SchemaEntry<int> tmpSchema = (SchemaEntry<int>) schemas[i];
                
                if (tmpCol.Width != 0) {
                    tmpSchema.Set (tmpCol.Width);
                }
            }
        }

        private int PodcastFeedTitleTreeIterCompareFunc (TreeModel model, TreeIter a,
                TreeIter b)
        {
            PodcastInfo pi_a = model.GetValue(a, 0) as PodcastInfo;
            PodcastInfo pi_b = model.GetValue(b, 0) as PodcastInfo;

            if (pi_a == null || pi_b == null)
            {
                return -1;
            }

            int title_compare = pi_a.Feed.CompareTo (pi_b.Feed);

            if (title_compare == 0)
            {
                return DateTime.Compare (pi_b.PubDate, pi_a.PubDate);
            }

            return title_compare;
        }

        private int PodcastTitleTreeIterCompareFunc (TreeModel model, TreeIter a,
                TreeIter b)
        {
            PodcastInfo pi_a = model.GetValue(a, 0) as PodcastInfo;
            PodcastInfo pi_b = model.GetValue(b, 0) as PodcastInfo;

            if (pi_a == null || pi_b == null)
                return -1;

            return String.Compare (pi_a.Title.ToLower (), pi_b.Title.ToLower ());
        }

        private int PodcastPubDateTreeIterCompareFunc (TreeModel model, TreeIter a,
                TreeIter b)
        {
            PodcastInfo pi_a = model.GetValue(a, 0) as PodcastInfo;
            PodcastInfo pi_b = model.GetValue(b, 0) as PodcastInfo;

            if (pi_a == null || pi_b == null)
                return -1;

            return DateTime.Compare (pi_a.PubDate, pi_b.PubDate);
        }

        private void SetRendererAttributes(CellRendererText renderer,
                                           string text, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti;
            renderer.Text = text;
            renderer.Foreground = null;

            PodcastInfo pi = tree_model.GetValue (iter, 0) as PodcastInfo;

            ti = pi.Track;
            bool is_streaming = false;

            if (PlayerEngineCore.CurrentTrack != null) {

                is_streaming = IsStreaming (pi);

                renderer.Weight = ((ti == PlayerEngineCore.CurrentTrack) || is_streaming)
                                  ? (int) Pango.Weight.Bold
                                  : (int) Pango.Weight.Normal;
            }

            if(ti != null || is_streaming) {
                renderer.Sensitive = true;
            } else {
                renderer.Sensitive = false;
            }
        }

        private void TrackCellPodcastTitle (TreeViewColumn tree_column,
                                            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            PodcastInfo pi = tree_model.GetValue (iter, 0) as PodcastInfo;

            if(pi == null)
            {
                return;
            }

            SetRendererAttributes(
                (CellRendererText) cell, pi.Title, tree_model, iter
            );
        }

        private void TrackCellPodcastFeedTitle (TreeViewColumn tree_column,
                                                CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            PodcastInfo pi = tree_model.GetValue (iter, 0) as PodcastInfo;

            if(pi == null)
            {
                return;
            }

            SetRendererAttributes(
                (CellRendererText) cell, pi.Feed.Title, tree_model, iter
            );
        }

        private void TrackCellPubDate (TreeViewColumn tree_column,
                                       CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            PodcastInfo pi = tree_model.GetValue (iter, 0) as PodcastInfo;

            if(pi == null)
            {
                return;
            }

            SetRendererAttributes(
                (CellRendererText) cell, pi.PubDate.ToString("d"), tree_model, iter
            );
        }

        private void TrackCellActivity (TreeViewColumn tree_column, CellRenderer cell,
                                        TreeModel tree_model, TreeIter iter)
        {
            PodcastInfo pi = tree_model.GetValue (iter, 0) as PodcastInfo;
            CellRendererPixbuf renderer = cell as CellRendererPixbuf;

            renderer.Pixbuf = null;
            renderer.StockId = null;           		
            renderer.Sensitive = true;

            if (IsStreaming (pi))
            {
                renderer.StockId = Stock.MediaPlay;
            } else if (pi.Track != null) {
                if (pi.Track.PlayCount == 0) {
                    renderer.Pixbuf = PodcastPixbufs.NewPodcastIcon;
                } else if (pi.Track == PlayerEngineCore.CurrentTrack)
                {
                    renderer.StockId = Stock.MediaPlay;
                }
            }
            else if (pi.DownloadInfo != null)
            {           		
                switch (pi.DownloadInfo.State)
                {
                    case DownloadState.Failed:
                        renderer.StockId = Stock.DialogError;
                        break;

                    case DownloadState.Ready:
                    case DownloadState.Paused:
                    case DownloadState.Queued:
                        renderer.StockId = Stock.GoForward;
                        renderer.Sensitive = false;
                        break;

                    case DownloadState.Canceled:
                    case DownloadState.CancelRequested:
                        renderer.StockId = Stock.Cancel;
                        break;

                    case DownloadState.Running:
                        renderer.StockId = Stock.GoForward;
                        break;
                    default:
                        break;
                }
            }
            else if (pi.DownloadFailed)
            {
                renderer.StockId = Stock.DialogError;
            }
        }

        private void DownloadCellToggle (TreeViewColumn tree_column, CellRenderer cell,
                                         TreeModel tree_model, TreeIter iter)
        {
            CellRendererToggle toggle = (CellRendererToggle)cell;

            PodcastInfo pi = tree_model.GetValue (iter, 0) as PodcastInfo;

            if (pi.Track != null)
            {
                toggle.Active = true;
                toggle.Sensitive = false;
            }
            else if (pi.DownloadInfo != null)
            {

                switch (pi.DownloadInfo.State)
                {
                    case DownloadState.Failed:
                        toggle.Active = false;
                        toggle.Sensitive = true;
                        break;

                    case DownloadState.Ready:
                    case DownloadState.Paused:
                    case DownloadState.Queued:
                    case DownloadState.Running:
                        toggle.Active = true;
                        toggle.Sensitive = true;
                        break;

                    case DownloadState.Completed:
                        toggle.Active = true;
                        toggle.Sensitive = false;
                        break;

                    case DownloadState.Canceled:
                    case DownloadState.CancelRequested:
                    case DownloadState.New:
                    default:
                        toggle.Active = false;
                        toggle.Sensitive = false;
                        break;
                }

            }
            else if (pi.IsDownloaded == true)
            {
                toggle.Active = true;
                toggle.Sensitive = false;                
            }
            else
            {
                toggle.Active = pi.IsQueued;
                toggle.Sensitive = true;
            }
        }

        public void FilterOnFeed (PodcastFeedInfo feed)
        {
            selected_feed = feed;
            Refilter ();
        }

        public void Refilter ()
        {
            GLib.Idle.Add (Filter);
        }

        private bool Filter ()
        {
            filter.Refilter ();
            ScrollToFirst ();
            return false;
        }

        private bool PodcastFilterFunc (TreeModel model, TreeIter iter)
        {
            // Should handle multiple selected feeds.
            PodcastInfo pi = model.GetValue(iter,0) as PodcastInfo;

            if (pi == null)
            {
                return false;
            }

            if (selected_feed.ID == pi.Feed.ID ||
                    selected_feed == PodcastFeedInfo.All)
            {
                if ((pi.Feed.IsSubscribed || pi.IsDownloaded) &&
                        pi.IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDownloadToggled(object o, ToggledArgs args)
        {
            try
            {
                CellRendererToggle renderer = (CellRendererToggle)o;

                if (!renderer.Sensitive)
                {
                    return;
                }

                TreeIter iter;

                if (!sort.GetIter(out iter, new TreePath (args.Path)))
                {
                    return;
                }

                PodcastInfo pi = sort.GetValue (iter, 0) as PodcastInfo;

                if (!pi.IsQueued)
                {
                    PodcastCore.QueuePodcastDownload (pi);
                }
                else
                {
                    PodcastCore.CancelPodcastDownload (pi);
                }

            }
            catch(Exception e)
            {
                Console.WriteLine (e.Message);
            }
        }

        private bool IsStreaming (PodcastInfo pi)
        {
            // TODO:  Change this, not safe
            if (pi.Url == null || PlayerEngineCore.CurrentSafeUri == null)
            {
                return false;
            }

            try
            {
                return (pi.Url.ToString () == PlayerEngineCore.CurrentSafeUri.ToString ());
            }
            catch {
                return false;
            }
        }

        // Everything below here is overly sepcific and will be replaced when a general
        // playlist standard is decided upon.

        public void GetPodcastModelPathAtPos (int x, int y, out TreePath path)
        {
            GetPathAtPos (x, y, out path);

            if (path == null)
            {
                return;
            }

            path = sort.ConvertPathToChildPath (path);
            path = filter.ConvertPathToChildPath (path);
        }

        public TreeIter GetPodcastModelIter (TreeIter iter)
        {
            if (iter.Equals (TreeIter.Zero))
            {
                return TreeIter.Zero;
            }

            iter = sort.ConvertIterToChildIter (iter);
            iter = filter.ConvertIterToChildIter (iter);

            return iter;
        }

        public void SelectPath (TreePath path)
        {
            if (path == null)
            {
                return;
            }

            path = filter.ConvertChildPathToPath (path);
            path = sort.ConvertChildPathToPath (path);

            if (path != null)
            {
                Selection.SelectPath (path);
            }
        }

        public TreePath GetPodcastModelPath (TreePath path)
        {
            if (path == null)
            {
                return null;
            }

            path = sort.ConvertPathToChildPath (path);
            path = filter.ConvertPathToChildPath (path);

            return path;
        }

        public TreePath[] GetPodcastModelPath (TreePath[] paths)
        {
            if (paths == null)
            {
                return null;
            }

            ArrayList model_paths = new ArrayList (paths.Length);

            foreach (TreePath tp in paths)
            {
                if (tp != null)
                {
                    TreePath p = GetPodcastModelPath (tp);
                    if (p != null)
                    {
                        model_paths.Add (p);
                    }
                }
            }

            if (model_paths.Count <= 0)
            {
                return null;
            }
            else
            {
                return model_paths.ToArray (typeof(TreePath)) as TreePath[];
            }
        }

        public void ScrollToFirst ()
        {
            TreeIter iter;

            sort.GetIterFirst (out iter);

            ScrollToIter (iter);
        }

        public void ScrollToIter (TreeIter iter)
        {
            if (iter.Equals (TreeIter.Zero) ||
                    !sort.IterIsValid (iter))
            {
                return;
            }

            TreePath path;

            path = sort.GetPath (iter);

            if (path != null)
            {
                ScrollToCell (path, null, false, 0, 0);
            }
        }
    }
}
