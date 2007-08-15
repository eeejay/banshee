/***************************************************************************
 *  RecommendationPane.cs
 *
 *  Copyright (C) 2006-2007 Novell, Inc.
 *  Written by Fredrik Hedberg
 *             Aaron Bockover
 *             Lukas Lipka
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
using System.Xml;
using System.Collections;

using Mono.Gettext;

using Gtk;

using Banshee.Base;
using Banshee.Widgets;

namespace Banshee.Plugins.Recommendation
{
    public class RecommendationPane : Frame
    {
        // NOTE: This is a precaution that will allow us to introduce changes in the cache system
        // without breaking the app when it expects new changes and encounters old cache data.
        // Whenever a fix or cache update is made in the code, increment this value to ensure 
        // old cache data is wiped first.
        private const int CACHE_VERSION = 2;
    
        private const string AUDIOSCROBBLER_SIMILAR_URL = "http://ws.audioscrobbler.com/1.0/artist/{0}/similar.xml";
        private const string AUDIOSCROBBLER_TOP_TRACKS_URL = "http://ws.audioscrobbler.com/1.0/artist/{0}/toptracks.xml";
        private const string AUDIOSCROBBLER_TOP_ALBUMS_URL = "http://ws.audioscrobbler.com/1.0/artist/{0}/topalbums.xml";

        private const int NUM_MAX_ARTISTS = 20;
        private const int NUM_TRACKS = 5;
        private const int NUM_ALBUMS = 5;

        private Box main_box, similar_box, tracks_box, albums_box;
        private Box tracks_items_box, albums_items_box;
        private ScrolledWindow similar_artists_view_sw;
        private MessagePane no_artists_pane;
        private TileView similar_artists_view;
        private Label tracks_header, albums_header;
        private ArrayList artists_widgets_list = new ArrayList();
        
        private string current_artist;
        public string CurrentArtist {
            get { return current_artist; }
        }

        public RecommendationPane() 
        {
            CreateWidget();
            CheckForCacheWipe();
            SetupCache();
        }

        private void CheckForCacheWipe()
        {
            bool wipe = false;
            
            if(!Directory.Exists(Utilities.CACHE_PATH)) {
                return;
            }
            
            if(RecommendationPlugin.CacheVersion.Get() < CACHE_VERSION) {
                Directory.Delete(Utilities.CACHE_PATH, true);
                LogCore.Instance.PushDebug("Recommendation Plugin", "Destroyed outdated cache");
            }
        }

        private void SetupCache()
        {
            bool clean = false;
            
            if(!Directory.Exists(Utilities.CACHE_PATH)) {
                clean = true;
                Directory.CreateDirectory(Utilities.CACHE_PATH);
            }
            
            // Create our cache subdirectories.
            for(int i = 0; i < 256; ++i) {
                string subdir = i.ToString("x");
                if(i < 16) {
                    subdir = "0" + subdir;
                }
                
                subdir = System.IO.Path.Combine(Utilities.CACHE_PATH, subdir);
                
                if(!Directory.Exists(subdir)) {
                    Directory.CreateDirectory(subdir);
                }
            }
            
            RecommendationPlugin.CacheVersion.Set(CACHE_VERSION);
            
            if(clean) {
                LogCore.Instance.PushDebug("Recommendation Plugin", "Created a new cache layout");
            }
        }

        private void CreateWidget()
        {
            ShadowType = ShadowType.In;
        
            EventBox event_box = new EventBox();
            
            main_box = new HBox();
            main_box.BorderWidth = 5;

            similar_box = new VBox(false, 3);
            tracks_box = new VBox(false, 3);
            albums_box = new VBox(false, 3);

            Label similar_header = new Label();
            similar_header.Xalign = 0;
            similar_header.Ellipsize = Pango.EllipsizeMode.End;
            similar_header.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText(
                Catalog.GetString("Recommended Artists")));
            similar_box.PackStart(similar_header, false, false, 0);

            tracks_header = new Label();
            tracks_header.Xalign = 0;
            tracks_header.WidthChars = 25;
            tracks_header.Ellipsize = Pango.EllipsizeMode.End;
            tracks_box.PackStart(tracks_header, false, false, 0);

            albums_header = new Label();
            albums_header.Xalign = 0;
            albums_header.WidthChars = 25;
            albums_header.Ellipsize = Pango.EllipsizeMode.End;
            albums_box.PackStart(albums_header, false, false, 0);

            similar_artists_view = new TileView(2);
            similar_artists_view_sw = new ScrolledWindow();
            similar_artists_view_sw.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            similar_artists_view_sw.Add(similar_artists_view);
            similar_artists_view_sw.ShowAll();
            similar_box.PackEnd(similar_artists_view_sw, true, true, 0);
            
            no_artists_pane = new MessagePane();
            string no_results_message;
            
            if(!Globals.ArgumentQueue.Contains("debug")) {
                no_artists_pane.HeaderIcon = IconThemeUtils.LoadIcon(48, "face-sad", Stock.DialogError);
                no_results_message = Catalog.GetString("No similar artists found");
            } else {
                no_artists_pane.HeaderIcon = Gdk.Pixbuf.LoadFromResource("no-results.png");
                no_results_message = "No one likes your music, fool!";
            }
            
            no_artists_pane.HeaderMarkup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText(no_results_message));
            similar_box.PackEnd(no_artists_pane, true, true, 0);
            
            tracks_items_box = new VBox(false, 0);
            tracks_box.PackEnd(tracks_items_box, true, true, 0);

            albums_items_box = new VBox(false, 0);
            albums_box.PackEnd(albums_items_box, true, true, 0);

            main_box.PackStart(similar_box, true, true, 5);
            main_box.PackStart(new VSeparator(), false, false, 0);
            main_box.PackStart(tracks_box, false, false, 5);
            main_box.PackStart(new VSeparator(), false, false, 0);
            main_box.PackStart(albums_box, false, false, 5);
            
            no_artists_pane.StyleSet += delegate {
                event_box.ModifyBg(StateType.Normal, no_artists_pane.Style.Base(StateType.Normal));
                similar_artists_view.ModifyBg(StateType.Normal, no_artists_pane.Style.Base(StateType.Normal));
            };
            
            event_box.Add(main_box);
            Add(event_box);
        }

        public void HideRecommendations()
        {
            Hide();
        }

        public void ShowRecommendations(string artist)
        {
            if(current_artist == artist) {
                Show();
                return;
            }
            
            Hide();
  
            ThreadAssist.Spawn(delegate {
                XmlNodeList artists, tracks, albums;
                try {
                    if(QueryRecommendationData(artist, out artists, out tracks, out albums)) {
                        ThreadAssist.ProxyToMain(delegate {
                            RenderRecommendationData(artist, artists, tracks, albums);
                        });
                    }
                } catch(Exception e) {
                    Console.Error.WriteLine("Could not fetch recommendations: {0}", e.Message);
                }
            });
        }
        
        private void RenderRecommendationData(string artist, XmlNodeList artistsXmlList, 
            XmlNodeList tracksXmlList, XmlNodeList albumsXmlList)
        {
            // Wipe the old recommendations here, we keep them around in case
            // where the the artist is the same as the last song.
            
            similar_artists_view.ClearWidgets();
                    
            foreach(Widget child in tracks_items_box.Children) {
                tracks_items_box.Remove(child);
            }
                    
            foreach(Widget child in albums_items_box.Children) {
                albums_items_box.Remove(child);
            }
                    
            // Display recommendations and artist information
            current_artist = artist;
            tracks_header.Markup = "<b>" + String.Format(Catalog.GetString("Top Tracks by {0}"), 
                GLib.Markup.EscapeText(artist)) + "</b>";
            albums_header.Markup = "<b>" + String.Format(Catalog.GetString("Top Albums by {0}"), 
                GLib.Markup.EscapeText(artist)) + "</b>";
                    
            artists_widgets_list.Clear();
            
            ShowAll();
            
            if(artistsXmlList != null && artistsXmlList.Count > 0) {
                for(int i = 0; i < artistsXmlList.Count && i < NUM_MAX_ARTISTS; i++) {
                    artists_widgets_list.Add(RenderSimilarArtist(artistsXmlList[i]));
                }
            
                RenderSimilarArtists();
                no_artists_pane.Hide();
                similar_artists_view_sw.ShowAll();
            } else {
                similar_artists_view_sw.Hide();
                no_artists_pane.ShowAll();
            }
            
            if(tracksXmlList != null) {
                for(int i = 0; i < tracksXmlList.Count && i < NUM_TRACKS; i++) {
                    tracks_items_box.PackStart(RenderTrack(tracksXmlList[i], i + 1), false, true, 0);
                }
                
                tracks_items_box.ShowAll();
            }    
            
            if(albumsXmlList != null) {
                for(int i = 0; i < albumsXmlList.Count && i < NUM_ALBUMS; i++) {
                    albums_items_box.PackStart(RenderAlbum(albumsXmlList[i], i + 1), false, true, 0);
                }
                
                albums_items_box.ShowAll();
            }
        }
        
        private bool QueryRecommendationData(string artist, out XmlNodeList artistsXmlList, 
            out XmlNodeList tracksXmlList, out XmlNodeList albumsXmlList)
        {
            // Last.fm requires double-encoding of all '/,&?' characters, see
            // http://bugzilla.gnome.org/show_bug.cgi?id=340511
            string encoded_artist = artist.Replace("/", "%2F").Replace (",", "%2C").Replace ("&", "%26").Replace ("?", "%3F");
            encoded_artist = System.Web.HttpUtility.UrlEncode(encoded_artist);

            // Fetch data for "similar" artists.
            XmlDocument artists_xml_data = new XmlDocument();
            artists_xml_data.LoadXml(Utilities.RequestContent(
                String.Format(AUDIOSCROBBLER_SIMILAR_URL, encoded_artist)));
            XmlNodeList artists_xml_list = artists_xml_data.SelectNodes("/similarartists/artist");

            // Cache artists images
            for(int i = 0; i < artists_xml_list.Count && i < NUM_MAX_ARTISTS; i++) {
                string url = artists_xml_list [i].SelectSingleNode("image_small").InnerText;                    
                Utilities.DownloadContent(url, Utilities.GetCachedPathFromUrl(url), true);
            }
                    
            // Fetch data for top tracks
            XmlDocument tracks_xml_data = new XmlDocument();
            tracks_xml_data.LoadXml(Utilities.RequestContent(
                String.Format(AUDIOSCROBBLER_TOP_TRACKS_URL, encoded_artist)));
            XmlNodeList tracks_xml_list = tracks_xml_data.SelectNodes("/mostknowntracks/track");                    
                
            // Try to match top tracks with the users's library
            for(int i = 0; i < tracks_xml_list.Count && i < NUM_TRACKS; i++) {
                string track_name = tracks_xml_list [i].SelectSingleNode("name").InnerText;
                int track_id = Utilities.GetTrackId(artist, track_name);
                    
                if(track_id == -1) {
                    continue;
                }
                
                XmlNode track_id_node = tracks_xml_list[i].OwnerDocument.CreateNode(
                    XmlNodeType.Element, "track_id", null);
                track_id_node.InnerText = track_id.ToString();
                
                tracks_xml_list[i].AppendChild(track_id_node);
            }
                
            // Fetch data for top albums
            XmlDocument albums_xml_data = new XmlDocument();
            albums_xml_data.LoadXml(Utilities.RequestContent(
                String.Format(AUDIOSCROBBLER_TOP_ALBUMS_URL, encoded_artist)));
            XmlNodeList albums_xml_list = albums_xml_data.SelectNodes("/topalbums/album");
            
            if(artists_xml_list.Count < 1 && tracks_xml_list.Count < 1 && albums_xml_list.Count < 1) {
                artistsXmlList = null;
                albumsXmlList = null;
                tracksXmlList = null;
            
                return false;
            }
            
            artistsXmlList = artists_xml_list;
            albumsXmlList = albums_xml_list;
            tracksXmlList = tracks_xml_list;
            
            return artist == PlayerEngineCore.CurrentTrack.Artist;
        }
        
        // --------------------------------------------------------------- //

        private void RenderSimilarArtists()
        {
            foreach(Widget artist in artists_widgets_list) {
                similar_artists_view.AddWidget(artist);
            }
        }
                
        private Widget RenderSimilarArtist(XmlNode node)
        {
            Tile artist_tile = new Tile();
            artist_tile.Pixbuf = RenderImage(node.SelectSingleNode("image_small").InnerText);
            artist_tile.PrimaryText = node.SelectSingleNode("name").InnerText.Trim();
            
            // translators: 25% similarity
            try {
                int similarity = (int)Math.Round(Double.Parse(node.SelectSingleNode("match").InnerText, 
                    Globals.InternalCultureInfo.NumberFormat));
                artist_tile.SecondaryText = String.Format(Catalog.GetString("{0}% Similarity"), similarity);
            } catch {
                artist_tile.SecondaryText = Catalog.GetString("Unknown Similarity");
            }

            artist_tile.Clicked += delegate {
                Gnome.Url.Show(node.SelectSingleNode ("url").InnerText);
            };

            return artist_tile;
        }

        private static Gdk.Pixbuf now_playing_arrow = IconThemeUtils.LoadIcon(16, "media-playback-start",
            Stock.MediaPlay, "now-playing-arrow");
        
        private static Gdk.Pixbuf unknown_artist_pixbuf = null;

        private Widget RenderTrack(XmlNode node, int rank)
        {
            Button track_button = new Button();
            track_button.Relief = ReliefStyle.None;

            HBox box = new HBox();

            Label label = new Label();
            label.Ellipsize = Pango.EllipsizeMode.End;
            label.Xalign = 0;
            label.Markup = String.Format("{0}. {1}", rank, GLib.Markup.EscapeText(
                node.SelectSingleNode("name").InnerText).Trim());

            if(node.SelectSingleNode("track_id") != null) {
                box.PackEnd(new Image(now_playing_arrow), false, false, 0);
                track_button.Clicked += delegate {
                    PlayerEngineCore.OpenPlay(Globals.Library.GetTrack(
                        Convert.ToInt32(node.SelectSingleNode("track_id").InnerText)));
                };
            } else {
                track_button.Clicked += delegate {
                    Gnome.Url.Show(node.SelectSingleNode("url").InnerText);
                };
            }

            box.PackStart(label, true, true, 0);
            track_button.Add(box);

            return track_button;
        }

        private Widget RenderAlbum(XmlNode node, int rank)
        {
            Button album_button = new Button();
            album_button.Relief = ReliefStyle.None;

            Label label = new Label ();
            label.Ellipsize = Pango.EllipsizeMode.End;
            label.Xalign = 0;
            label.Markup = String.Format("{0}. {1}", rank, GLib.Markup.EscapeText(
                node.SelectSingleNode("name").InnerText).Trim());
            album_button.Add(label);

            album_button.Clicked += delegate {
                Gnome.Url.Show(node.SelectSingleNode("url").InnerText);
            };

            return album_button;
        }

        private Gdk.Pixbuf RenderImage(string url)
        {
            string path = Utilities.GetCachedPathFromUrl(url);
            Utilities.DownloadContent(url, path, true);
            
            try {
                return new Gdk.Pixbuf(path);
            } catch {
                // Remove the corrupt image so it may be downloaded again
                try {
                    File.Delete(path);
                } catch {
                }
                
                if(unknown_artist_pixbuf == null) {
                    unknown_artist_pixbuf = Gdk.Pixbuf.LoadFromResource("generic-artist.png");
                }
                
                return unknown_artist_pixbuf;
            }
        }
    }
}
