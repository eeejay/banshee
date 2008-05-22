/*************************************************************************** 
 *  ItemsTableManager.cs
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

namespace Migo.Syndication.Data
{    
    /*static class ItemsTableManager
    {        
        public static readonly string initQuery = @" 
            CREATE TABLE IF NOT EXISTS items (
            	'local_id' INTEGER  PRIMARY KEY, 
            	'parent_id' INTEGER NOT NULL DEFAULT '-1',
            	'active' INTEGER NOT NULL DEFAULT '0', 
            	'author' TEXT NOT NULL DEFAULT '', 
            	'comments' TEXT NOT NULL DEFAULT '', 
            	'description' TEXT NOT NULL DEFAULT '', 
            	'guid' TEXT NOT NULL DEFAULT '', 
            	'is_read' INTEGER NOT NULL DEFAULT '0',
            	'last_download_time' DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00', 
            	'link' TEXT NOT NULL DEFAULT '', 
            	'modified' DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00', 
            	'pubdate' DATETIME NOT NULL DEFAULT '0001-01-01 00:00:00', 	
            	'title' TEXT NOT NULL DEFAULT ''	
            );

            CREATE TRIGGER IF NOT EXISTS item_deleted_trigger
               AFTER DELETE ON items
            BEGIN
                DELETE FROM enclosures WHERE parent_id=old.local_id;
            END;
            
            CREATE INDEX IF NOT EXISTS items_local_id_index ON items(local_id);
            CREATE INDEX IF NOT EXISTS items_parent_id_index ON items(parent_id);                                                       
        ";
        
        private const string deactivateItemsBaseQuery = "UPDATE items SET active='0' WHERE";        
        private static readonly string deleteFromItemsBaseQuery = "DELETE FROM items WHERE";        
        private static readonly string insertIntoItemsQuery = 
            String.Format ( 
                @"INSERT INTO items VALUES (
                    NULL, {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}
                ); {12}",
                DbDefines.ItemsTableColumns.ParentIDParameter, 
                DbDefines.ItemsTableColumns.ActiveParameter, DbDefines.ItemsTableColumns.AuthorParameter,                
                DbDefines.ItemsTableColumns.CommentsParameter, DbDefines.ItemsTableColumns.DescriptionParameter, 
                DbDefines.ItemsTableColumns.GuidParameter, DbDefines.ItemsTableColumns.IsReadParameter, 
                DbDefines.ItemsTableColumns.LastDownloadTimeParameter, DbDefines.ItemsTableColumns.LinkParameter, 
                DbDefines.ItemsTableColumns.ModifiedParameter, DbDefines.ItemsTableColumns.PubDateParameter, 
                DbDefines.ItemsTableColumns.TitleParameter, DbDefines.LastInsertIDQuery
            );

        private static readonly string updateItemsQuery = 
            String.Format (
                @"UPDATE items SET 
                    {2}={3}, {4}={5}, {6}={7}, {8}={9}, {10}={11}, {12}={13}, 
                    {14}={15}, {16}={17}, {18}={19}, {20}={21}, {22}={23}, {24}={25}
                WHERE {0}={1};",
                DbDefines.ItemsTableColumns.LocalID, DbDefines.ItemsTableColumns.LocalIDParameter,                           
                DbDefines.ItemsTableColumns.ParentID, DbDefines.ItemsTableColumns.ParentIDParameter, 
                DbDefines.ItemsTableColumns.Active, DbDefines.ItemsTableColumns.ActiveParameter, 
                DbDefines.ItemsTableColumns.Author, DbDefines.ItemsTableColumns.AuthorParameter, 
                DbDefines.ItemsTableColumns.Comments, DbDefines.ItemsTableColumns.CommentsParameter, 
                DbDefines.ItemsTableColumns.Description, DbDefines.ItemsTableColumns.DescriptionParameter, 
                DbDefines.ItemsTableColumns.Guid, DbDefines.ItemsTableColumns.GuidParameter, 
                DbDefines.ItemsTableColumns.IsRead, DbDefines.ItemsTableColumns.IsReadParameter, 
                DbDefines.ItemsTableColumns.LastDownloadTime, DbDefines.ItemsTableColumns.LastDownloadTimeParameter, 
                DbDefines.ItemsTableColumns.Link, DbDefines.ItemsTableColumns.LinkParameter, 
                DbDefines.ItemsTableColumns.Modified, DbDefines.ItemsTableColumns.ModifiedParameter,
                DbDefines.ItemsTableColumns.PubDate, DbDefines.ItemsTableColumns.PubDateParameter, 
                DbDefines.ItemsTableColumns.Title, DbDefines.ItemsTableColumns.TitleParameter
            );
        
        public static void Commit (IEnumerable<FeedItem> items)
        {
            if (items == null) {
                throw new ArgumentNullException ("items");
            }    

            FeedEnclosure tmpEnc;
            ICollection<FeedItem> itemCol = items as ICollection<FeedItem>;
            
            Dictionary<QueuedDbCommand,FeedItem> itemCommandPairs = (itemCol == null) ?
                new Dictionary<QueuedDbCommand,FeedItem> () : 
                new Dictionary<QueuedDbCommand,FeedItem> (itemCol.Count);
            
            List<FeedEnclosure> enclosures = 
                new List<FeedEnclosure> (itemCommandPairs.Count);
            
            foreach (FeedItem i in items) {
                itemCommandPairs.Add (QueuedDbCommand.CreateScalar (
                    CreateInsertCommandImpl (i)), i
                );
            }
            
            foreach (FeedItem i in items) {                
                if (i.Enclosure != null) {
                    tmpEnc = i.Enclosure as FeedEnclosure;
                    if (tmpEnc != null) {
                        enclosures.Add (tmpEnc);
                    }
                }            
            }            
            
            DatabaseManager.Enqueue (itemCommandPairs.Keys);
            
            foreach (KeyValuePair<QueuedDbCommand,FeedItem> kvp in itemCommandPairs) {
                kvp.Value.LocalID = Convert.ToInt64 (kvp.Key.ScalarResult);
            }
                            
            if (enclosures.Count > 0) {
                EnclosuresTableManager.Commit (enclosures);
            }
        }
        
        public static IDbCommand CreateInsertCommand (FeedItem item)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");
            }
            
            return CreateInsertCommandImpl (item);
        }
        
        private static IDbCommand CreateInsertCommandImpl (FeedItem item)
        {
            return DatabaseManager.CreateCommand (
                insertIntoItemsQuery,
                DbDefines.ItemsTableColumns.ParentID, item.Parent.LocalID.ToString (),
                DbDefines.ItemsTableColumns.Active, (item.Active) ? "1" : "0",                                              
                DbDefines.ItemsTableColumns.Author, item.Author, 
                DbDefines.ItemsTableColumns.Comments, item.Comments, 
                DbDefines.ItemsTableColumns.Description, item.Description, 
                DbDefines.ItemsTableColumns.Guid, item.Guid.ToString (), 
                DbDefines.ItemsTableColumns.IsRead, item.IsRead.ToString (), 
                DbDefines.ItemsTableColumns.LastDownloadTime, item.LastDownloadTime.ToUniversalTime ().ToString ("u"), 
                DbDefines.ItemsTableColumns.Link, item.Link, 
                DbDefines.ItemsTableColumns.Modified, item.Modified.ToUniversalTime ().ToString ("u"),
                DbDefines.ItemsTableColumns.PubDate, item.PubDate.ToUniversalTime ().ToString ("u"), 
                DbDefines.ItemsTableColumns.Title, item.Title            
            );
        }        
        
        public static void Deactivate (FeedItem item)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");
            }    
            
             DatabaseManager.ExecuteNonQuery (
                String.Format (
                    "{0} {1}={2};",
                    deactivateItemsBaseQuery, 
                    DbDefines.ItemsTableColumns.LocalID, 
                    DbDefines.ItemsTableColumns.LocalIDParameter
                )
            );                 
        }
        
        public static void Deactivate (IEnumerable<FeedItem> items)
        {
            if (items == null) {
                throw new ArgumentNullException ("items");
            }        

            List<long> ids = new List<long> ();
            
            foreach (FeedItem i in items) {
                if (i != null) {
                    ids.Add (i.LocalID);
                }
            }
            
            if (ids.Count > 0) {
                string query = DataUtility.MultipleOnIDQuery (
                    deactivateItemsBaseQuery, 
                    DbDefines.ItemsTableColumns.LocalID, 
                    ids.ToArray ()
                );
                
                DatabaseManager.ExecuteNonQuery (query);
            }      
        }        
        
        public static void Delete (FeedItem item)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");
            }
                        
            DatabaseManager.ExecuteNonQuery (
                String.Format (
                    "{0} {1}={2};",
                    deleteFromItemsBaseQuery, 
                    DbDefines.ItemsTableColumns.LocalID, 
                    DbDefines.ItemsTableColumns.LocalIDParameter
                )
            );
        }
        
        public static void Delete (IEnumerable<FeedItem> items)
        {
            if (items == null) {
            	throw new ArgumentNullException ("items");
            }
            
            List<long> ids = new List<long> ();
            
            foreach (FeedItem i in items) {
                if (i != null) {
                    ids.Add (i.LocalID);
                }
            }
            
            if (ids.Count > 0) {
                string query = DataUtility.MultipleOnIDQuery (
                    deleteFromItemsBaseQuery, DbDefines.ItemsTableColumns.LocalID, ids.ToArray ()
                );
                
                DatabaseManager.ExecuteNonQuery (query);
            }
        }           
     
        public static void Init ()
        {
            DatabaseManager.ExecuteNonQuery (initQuery);               
        }        
        
        public static long Insert (FeedItem item)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");
            } else if (item.LocalID > 0) {
                return item.LocalID;
            }   

            long ret = -1;
            
            if (item.Parent == null) {
                return ret;
            }
            
            ret = Convert.ToInt64 (CreateInsertCommandImpl (item));
                        
            return ret;
        }

        public static bool Insert (IEnumerable<FeedItem> items)
        {
            if (items == null) {
                throw new ArgumentNullException ("items");
            }    
            
            bool ret = false;
        
            try {
                foreach (FeedItem i in items) {                
                    Insert (i);
                }
                
                ret = true;
            } catch {}   
        
            return ret;
        }

        public static void Update (FeedItem item)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");       
            }   
            
            DatabaseManager.ExecuteNonQuery (
                updateItemsQuery,
                DbDefines.ItemsTableColumns.LocalID, item.LocalID.ToString (),                
                DbDefines.ItemsTableColumns.ParentID, item.Parent.LocalID.ToString (), 
                DbDefines.ItemsTableColumns.Active, (item.Active) ? "1" : "0",                 
                DbDefines.ItemsTableColumns.Author, item.Author, 
                DbDefines.ItemsTableColumns.Comments, item.Comments, 
                DbDefines.ItemsTableColumns.Description, item.Description, 
                DbDefines.ItemsTableColumns.Guid, item.Guid.ToString (), 
                DbDefines.ItemsTableColumns.IsRead, item.IsRead.ToString (), 
                DbDefines.ItemsTableColumns.LastDownloadTime, item.LastDownloadTime.ToUniversalTime ().ToString ("u"), 
                DbDefines.ItemsTableColumns.Link, item.Link, 
                DbDefines.ItemsTableColumns.Modified, item.Modified.ToUniversalTime ().ToString ("u"),
                DbDefines.ItemsTableColumns.PubDate, item.PubDate.ToUniversalTime ().ToString ("u"), 
                DbDefines.ItemsTableColumns.Title, item.Title
            );
        }
        
        public static bool Update (IEnumerable<FeedItem> items)
        {
            if (items == null) {
                throw new ArgumentNullException ("items");
            }    
            
            bool ret = false;
            
            try {
                foreach (FeedItem i in items) {                
                    Update (i);
                }
                
                ret = true;
            } catch {}     
            
            return ret;
        }        
    }*/
}
