/*************************************************************************** 
 *  FeedsTableManager.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
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
using System.Data;

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Migo.Syndication.Data
{    
    class FeedsTableManager
    {     
        public static readonly string initQuery = @" 
            CREATE TABLE IF NOT EXISTS feeds (
            	'local_id' INTEGER  PRIMARY KEY, 
            	'copyright' TEXT NOT NULL DEFAULT '', 
            	'description' TEXT NOT NULL DEFAULT '', 
            	'download_enclosures_automatically' INTEGER NOT NULL DEFAULT '0', 
            	'download_url' TEXT NOT NULL DEFAULT '', 
            	'image' TEXT NOT NULL DEFAULT '', 
            	'interval' INTEGER NOT NULL DEFAULT '1440', 
            	'is_list' INTEGER NOT NULL DEFAULT '0', 
            	'language' TEXT NOT NULL DEFAULT '', 
            	'last_build_date' DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00', 
            	'last_download_error' INTEGER NOT NULL DEFAULT '0', 
            	'last_download_time' DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00', 
            	'last_write_time' DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00', 
            	'link' TEXT NOT NULL DEFAULT '', 
            	'local_enclosure_path' TEXT NOT NULL DEFAULT '', 
            	'max_item_count' INTEGER NOT NULL DEFAULT '200', 
            	'name' TEXT NOT NULL DEFAULT '', 
            	'pubdate' DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00', 
            	'sync_setting' INTEGER NOT NULL DEFAULT '0', 
            	'title' TEXT NOT NULL DEFAULT '', 
            	'ttl' INTEGER NOT NULL DEFAULT '0', 
            	'url' TEXT NOT NULL DEFAULT ''
            );
            
            CREATE TRIGGER IF NOT EXISTS feed_deleted_trigger
               AFTER DELETE ON feeds
            BEGIN
                DELETE FROM items WHERE parent_id=old.local_id;
            END;

            CREATE INDEX IF NOT EXISTS feeds_local_id_index ON feeds(local_id);";

        private static readonly string deleteFromFeedsBaseQuery = "DELETE FROM feeds WHERE";        
        private static readonly string insertIntoFeedsQuery = 
            String.Format ( 
                @"INSERT INTO feeds VALUES (
                    NULL, {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, 
                    {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20}
                ); {21}",
                DbDefines.FeedsTableColumns.CopyrightParameter, DbDefines.FeedsTableColumns.DescriptionParameter, 
                DbDefines.FeedsTableColumns.DownloadEnclosuresAutomaticallyParameter,
                DbDefines.FeedsTableColumns.DownloadUrlParameter, DbDefines.FeedsTableColumns.ImageParameter,
                DbDefines.FeedsTableColumns.IntervalParameter, DbDefines.FeedsTableColumns.IsListParameter,
                DbDefines.FeedsTableColumns.LanguageParameter, DbDefines.FeedsTableColumns.LastBuildDateParameter,
                DbDefines.FeedsTableColumns.LastDownloadErrorParameter, DbDefines.FeedsTableColumns.LastDownloadTimeParameter,
                DbDefines.FeedsTableColumns.LastWriteTimeParameter, DbDefines.FeedsTableColumns.LinkParameter,
                DbDefines.FeedsTableColumns.LocalEnclosurePathParameter, DbDefines.FeedsTableColumns.MaxItemCountParameter,
                DbDefines.FeedsTableColumns.NameParameter, DbDefines.FeedsTableColumns.PubDateParameter,
                DbDefines.FeedsTableColumns.SyncSettingParameter, DbDefines.FeedsTableColumns.TitleParameter,
                DbDefines.FeedsTableColumns.TtlParameter, DbDefines.FeedsTableColumns.UrlParameter, 
                DbDefines.LastInsertIDQuery
            );
                           
        private static readonly string selectAllFeedsQuery = "SELECT * FROM feeds;";
        private static readonly string selectAllFeedItemsQuery = "SELECT * FROM items;";      
        private static readonly string selectAllFeedEnclosuresQuery = "SELECT * FROM enclosures;";                
             
        private static readonly string updateFeedQuery = 
            String.Format (
                @"UPDATE feeds SET 
                    {2}={3}, {4}={5}, {6}={7}, {8}={9}, {10}={11}, {12}={13}, {14}={15}, {16}={17},
                    {18}={19}, {20}={21}, {22}={23}, {24}={25}, {26}={27}, {28}={29}, {30}={31},
                    {32}={33}, {34}={35}, {36}={37}, {38}={39}, {40}={41}, {42}={43} 
                    WHERE {0}={1};",
                    DbDefines.FeedsTableColumns.LocalID, DbDefines.FeedsTableColumns.LocalIDParameter,                                                     
                    DbDefines.FeedsTableColumns.Copyright, DbDefines.FeedsTableColumns.CopyrightParameter, 
                    DbDefines.FeedsTableColumns.Description, DbDefines.FeedsTableColumns.DescriptionParameter, 
                    DbDefines.FeedsTableColumns.DownloadEnclosuresAutomatically,                    
                    DbDefines.FeedsTableColumns.DownloadEnclosuresAutomaticallyParameter,
                    DbDefines.FeedsTableColumns.DownloadUrl, DbDefines.FeedsTableColumns.DownloadUrlParameter, 
                    DbDefines.FeedsTableColumns.Image, DbDefines.FeedsTableColumns.ImageParameter,
                    DbDefines.FeedsTableColumns.Interval, DbDefines.FeedsTableColumns.IntervalParameter, 
                    DbDefines.FeedsTableColumns.IsList, DbDefines.FeedsTableColumns.IsListParameter,
                    DbDefines.FeedsTableColumns.Language, DbDefines.FeedsTableColumns.LanguageParameter, 
                    DbDefines.FeedsTableColumns.LastBuildDate, DbDefines.FeedsTableColumns.LastBuildDateParameter,
                    DbDefines.FeedsTableColumns.LastDownloadError, DbDefines.FeedsTableColumns.LastDownloadErrorParameter, 
                    DbDefines.FeedsTableColumns.LastDownloadTime, DbDefines.FeedsTableColumns.LastDownloadTimeParameter,
                    DbDefines.FeedsTableColumns.LastWriteTime, DbDefines.FeedsTableColumns.LastWriteTimeParameter, 
                    DbDefines.FeedsTableColumns.Link, DbDefines.FeedsTableColumns.LinkParameter,
                    DbDefines.FeedsTableColumns.LocalEnclosurePath, DbDefines.FeedsTableColumns.LocalEnclosurePathParameter, 
                    DbDefines.FeedsTableColumns.MaxItemCount, DbDefines.FeedsTableColumns.MaxItemCountParameter,
                    DbDefines.FeedsTableColumns.Name, DbDefines.FeedsTableColumns.NameParameter, 
                    DbDefines.FeedsTableColumns.PubDate, DbDefines.FeedsTableColumns.PubDateParameter,
                    DbDefines.FeedsTableColumns.SyncSetting, DbDefines.FeedsTableColumns.SyncSettingParameter, 
                    DbDefines.FeedsTableColumns.Title, DbDefines.FeedsTableColumns.TitleParameter,
                    DbDefines.FeedsTableColumns.Ttl, DbDefines.FeedsTableColumns.TtlParameter, 
                    DbDefines.FeedsTableColumns.Url, DbDefines.FeedsTableColumns.UrlParameter
            );
      
        
        public static bool Commit (IEnumerable<Feed> feeds)
        {
            if (feeds == null) {
                throw new ArgumentNullException ("feeds");
            }    
            
            bool ret = false;
            
            try {
                foreach (Feed f in feeds) {                
                    f.Commit ();
                }
                
                ret = true;
            } catch {} 
            
            return ret;
        }          
        
        public static List<Feed> GetAllFeeds (FeedsManager manager)
        {
            if (manager == null) {
                throw new ArgumentNullException ("manager");
            }
            
            DateTime start = DateTime.Now;
            Console.WriteLine (start);
            
            List<Feed> feeds;       
            Dictionary<long,ICollection<FeedItem>> feedItems;
            Dictionary<long,FeedEnclosure> feedEnclosures;               
                        
            feedEnclosures = BuildEnclosuresDict ();            
            feedItems = BuildItemsDict (feedEnclosures);
            feeds = BuildFeedsList (manager, feedItems);
                        
            feedItems.Clear ();
            feedEnclosures.Clear ();
            
            Console.WriteLine (DateTime.Now - start);
            return feeds;    
        }

        public static void Delete (Feed feed)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");
            }

            DatabaseManager.ExecuteNonQuery (
                String.Format (
                    "{0} {1}={2};",
                    deleteFromFeedsBaseQuery, 
                    DbDefines.FeedsTableColumns.LocalID, 
                    DbDefines.FeedsTableColumns.LocalIDParameter
                ), 
                DbDefines.FeedsTableColumns.LocalID, 
                feed.LocalID.ToString ()
            );
        }
        
        public static void Delete (IEnumerable<Feed> feeds)
        {
            if (feeds == null) {
            	throw new ArgumentNullException ("feeds");
            }
            
            List<long> ids = new List<long> ();
            
            foreach (Feed f in feeds) {
                if (f != null) {
                    ids.Add (f.LocalID);
                }
            }
            
            if (ids.Count > 0) {
                string query = DataUtility.MultipleOnIDQuery (
                    deleteFromFeedsBaseQuery, DbDefines.FeedsTableColumns.LocalID, ids.ToArray ()
                );
                
                DatabaseManager.ExecuteNonQuery (query);
            }
        }        
        
        public static void Init ()
        {
            DatabaseManager.ExecuteNonQuery (initQuery);               
        }
        
        public static long Insert (Feed feed)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");
            } else if (feed.LocalID > 0) {
                return feed.LocalID;
            }   
            
            long ret = -1;
                        
            try {            
                ret = Convert.ToInt64 (
                    DatabaseManager.ExecuteScalar (                
                        insertIntoFeedsQuery,
                        DbDefines.FeedsTableColumns.Copyright, feed.Copyright == null ? String.Empty : feed.Copyright,
                        DbDefines.FeedsTableColumns.Description, feed.Description == null ? String.Empty : feed.Description,
                        DbDefines.FeedsTableColumns.DownloadEnclosuresAutomatically.ToString (), 
                        feed.DownloadEnclosuresAutomatically.ToString (),  
                        DbDefines.FeedsTableColumns.DownloadUrl, feed.DownloadUrl == null ? String.Empty : feed.DownloadUrl,
                        DbDefines.FeedsTableColumns.Image, feed.Image == null ? String.Empty : feed.Image,
                        DbDefines.FeedsTableColumns.Interval, feed.Interval.ToString (),
                        DbDefines.FeedsTableColumns.IsList, feed.IsList.ToString (),
                        DbDefines.FeedsTableColumns.Language, feed.Language == null ? String.Empty : feed.Language,
                        DbDefines.FeedsTableColumns.LastBuildDate, feed.LastBuildDate.ToUniversalTime ().ToString ("u"),
                        DbDefines.FeedsTableColumns.LastDownloadError, ((int)feed.LastDownloadError).ToString (),
                        DbDefines.FeedsTableColumns.LastDownloadTime, feed.LastDownloadTime.ToUniversalTime ().ToString ("u"),
                        DbDefines.FeedsTableColumns.LastWriteTime, feed.LastWriteTime.ToUniversalTime ().ToString ("u"),
                        DbDefines.FeedsTableColumns.Link, feed.Link == null ? String.Empty : feed.Link,
                        DbDefines.FeedsTableColumns.LocalEnclosurePath, feed.LocalEnclosurePath == null ? String.Empty : feed.LocalEnclosurePath,  
                        DbDefines.FeedsTableColumns.MaxItemCount, feed.MaxItemCount.ToString (),
                        DbDefines.FeedsTableColumns.Name, feed.Name == null ? String.Empty : feed.Name,
                        DbDefines.FeedsTableColumns.PubDate, feed.PubDate.ToUniversalTime ().ToString ("u"), 
                        DbDefines.FeedsTableColumns.SyncSetting, ((int)feed.SyncSetting).ToString (),
                        DbDefines.FeedsTableColumns.Title, feed.Title == null ? String.Empty : feed.Title,
                        DbDefines.FeedsTableColumns.Ttl, feed.Ttl.ToString (),
                        DbDefines.FeedsTableColumns.Url, feed.Url == null ? String.Empty : feed.Url
                    )
                );
            } catch {}
            
            Console.WriteLine ("feed.LocalID:  {0}", ret);
            
            return ret;
        }
        
        public static bool Insert (IEnumerable<Feed> feeds)
        {
            if (feeds == null) {
                throw new ArgumentNullException ("feeds");
            }    
            
            bool ret = false;
            
            try {
                foreach (Feed f in feeds) {                
                    Insert (f);
                }
                
                ret = true;
            } catch {}
        
            return ret;
        }          
        
        public static void Update (Feed feed)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");       
            }   

            DatabaseManager.ExecuteNonQuery (
                updateFeedQuery,
                DbDefines.FeedsTableColumns.LocalID, feed.LocalID.ToString (),                                             
                DbDefines.FeedsTableColumns.Copyright, feed.Copyright == null ? String.Empty : feed.Copyright,
                DbDefines.FeedsTableColumns.Description, feed.Description == null ? String.Empty : feed.Description,
                DbDefines.FeedsTableColumns.DownloadEnclosuresAutomatically.ToString (), 
                feed.DownloadEnclosuresAutomatically.ToString (),  
                DbDefines.FeedsTableColumns.DownloadUrl, feed.DownloadUrl == null ? String.Empty : feed.DownloadUrl,
                DbDefines.FeedsTableColumns.Image, feed.Image == null ? String.Empty : feed.Image,
                DbDefines.FeedsTableColumns.Interval, feed.Interval.ToString (),
                DbDefines.FeedsTableColumns.IsList, feed.IsList.ToString (),
                DbDefines.FeedsTableColumns.Language, feed.Language == null ? String.Empty : feed.Language,
                DbDefines.FeedsTableColumns.LastBuildDate, feed.LastBuildDate.ToUniversalTime ().ToString ("u"),
                DbDefines.FeedsTableColumns.LastDownloadError, ((int)feed.LastDownloadError).ToString (),
                DbDefines.FeedsTableColumns.LastDownloadTime, feed.LastDownloadTime.ToUniversalTime ().ToString ("u"),
                DbDefines.FeedsTableColumns.LastWriteTime, feed.LastWriteTime.ToUniversalTime ().ToString ("u"),
                DbDefines.FeedsTableColumns.Link, feed.Link == null ? String.Empty : feed.Link,
                DbDefines.FeedsTableColumns.LocalEnclosurePath, feed.LocalEnclosurePath == null ? String.Empty : feed.LocalEnclosurePath,  
                DbDefines.FeedsTableColumns.MaxItemCount, feed.MaxItemCount.ToString (),
                DbDefines.FeedsTableColumns.Name, feed.Name == null ? String.Empty : feed.Name,
                DbDefines.FeedsTableColumns.PubDate, feed.PubDate.ToUniversalTime ().ToString ("u"), 
                DbDefines.FeedsTableColumns.SyncSetting, ((int)feed.SyncSetting).ToString (),
                DbDefines.FeedsTableColumns.Title, feed.Title == null ? String.Empty : feed.Title,
                DbDefines.FeedsTableColumns.Ttl, feed.Ttl.ToString (),
                DbDefines.FeedsTableColumns.Url, feed.Url == null ? String.Empty : feed.Url
            );
        }
        
        public static bool Update (IEnumerable<Feed> feeds)
        {
            if (feeds == null) {
                throw new ArgumentNullException ("feeds");
            }    
            
            bool ret = false;
            
            try {
                foreach (Feed f in feeds) {                
                    Update (f);
                    ret = true;
                }
            } catch {}
           
            return ret;
        }           
        
        private static Dictionary<long,FeedEnclosure> BuildEnclosuresDict ()
        {
            IDataReader reader = null;            
            Dictionary<long,FeedEnclosure> ret = 
                new Dictionary<long,FeedEnclosure> ();            
            
            try {
                FeedEnclosure tmpFE;
                reader = DatabaseManager.ExecuteReader (selectAllFeedEnclosuresQuery);
                FeedEnclosureDataWrapper fedw = new FeedEnclosureDataWrapper (reader);
                        
                while (fedw.Read ()) {
                    tmpFE = new FeedEnclosure (fedw);
                    ret.Add (fedw.ParentID, tmpFE);
                }
            } finally {                
                if (reader != null) {
                    reader.Dispose ();
                    reader = null;                    
                }
            }
            
            return ret;
        }
        
        private static Dictionary<long,ICollection<FeedItem>> 
            BuildItemsDict (Dictionary<long,FeedEnclosure> feedEnclosures)
        {
            IDataReader reader = null;            
            Dictionary<long,ICollection<FeedItem>> ret =
                new Dictionary<long,ICollection<FeedItem>> ();
            
            try {
                FeedItem tmpItem;            
                
                DateTime start = DateTime.Now;
                reader = DatabaseManager.ExecuteReader (selectAllFeedItemsQuery);
                Console.WriteLine ("Time to select all feed items:  {0}", DateTime.Now-start);

                FeedItemDataWrapper fidw = new FeedItemDataWrapper (reader);
                        
                while (fidw.Read ()) {
                    tmpItem = new FeedItem (fidw);
                            
                    if (!ret.ContainsKey (fidw.ParentID)) {
                        ret.Add (fidw.ParentID, new List<FeedItem> ());
                    }
                        
                    ret[fidw.ParentID].Add (tmpItem);
                    
                    if (feedEnclosures.ContainsKey (tmpItem.LocalID)) {
                        tmpItem.Enclosure = feedEnclosures[tmpItem.LocalID];
                    }                       
                } 
            } finally {                
                if (reader != null) {
                    reader.Dispose ();
                    reader = null;                    
                }
            } 
  
            return ret;
        }
        
        private static List<Feed> BuildFeedsList (FeedsManager manager, Dictionary<long,ICollection<FeedItem>> feedItems)
        {
            IDataReader reader = null;            
            List<Feed> ret = new List<Feed> ();
            
            try {
                Feed tmpFeed;
                ICollection<FeedItem> tmpCollection;
                
                reader = DatabaseManager.ExecuteReader (selectAllFeedsQuery);                       
                FeedDataWrapper fdw = new FeedDataWrapper (reader);
                      
                while (fdw.Read ()) {
                    tmpFeed = new Feed (manager, fdw);

                    if (feedItems.TryGetValue (tmpFeed.LocalID, out tmpCollection)) {
                        tmpFeed.SetItems (tmpCollection);                        
                    }

                    ret.Add (tmpFeed);                        
                }   
            } finally {                
                if (reader != null) {
                    reader.Dispose ();
                    reader = null;                    
                }
            }       
            
            return ret;
        }    
    }
}
