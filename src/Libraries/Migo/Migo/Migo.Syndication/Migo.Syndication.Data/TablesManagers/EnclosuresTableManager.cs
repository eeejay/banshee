/*************************************************************************** 
 *  EnclosuresTableManager.cs
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
    static class EnclosuresTableManager
    {        
        private const string initQuery = @"
            CREATE TABLE IF NOT EXISTS enclosures (
            	'local_id' INTEGER  PRIMARY KEY, 
            	'parent_id' INTEGER NOT NULL DEFAULT '-1',	
            	'active' INTEGER NOT NULL DEFAULT '0', 
            	'download_mime_type' TEXT NOT NULL DEFAULT '',
                'download_url' TEXT NOT NULL DEFAULT '',
                'last_download_error' INTEGER NOT NULL DEFAULT '0',
            	'length' INTEGER NOT NULL DEFAULT '0',
            	'local_path' TEXT NOT NULL DEFAULT '',	
            	'type' TEXT NOT NULL DEFAULT '',
            	'url' TEXT NOT NULL DEFAULT ''
            );
            
            CREATE INDEX IF NOT EXISTS enclosures_local_id_index ON enclosures(local_id);
            CREATE INDEX IF NOT EXISTS enclosures_parent_id_index ON enclosures(parent_id);            
        ";
        
        private static readonly string insertIntoEnclosuresQuery =             
            String.Format ( 
                @"INSERT INTO enclosures VALUES (
                    NULL, {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}
                ); {9}",
                DbDefines.EnclosuresTableColumns.ParentIDParameter, 
                DbDefines.EnclosuresTableColumns.ActiveParameter,
                DbDefines.EnclosuresTableColumns.DownloadMimeTypeParameter, 
                DbDefines.EnclosuresTableColumns.DownloadUrlParameter, 
                DbDefines.EnclosuresTableColumns.LastDownloadErrorParameter,
                DbDefines.EnclosuresTableColumns.LengthParameter,
                DbDefines.EnclosuresTableColumns.LocalPathParameter,
                DbDefines.EnclosuresTableColumns.TypeParameter,
                DbDefines.EnclosuresTableColumns.UrlParameter,
                DbDefines.LastInsertIDQuery
            );

        private static readonly string updateEnclosuresQuery = 
            String.Format (
                @"UPDATE enclosures SET 
                    {2}={3}, {4}={5}, {6}={7}, {8}={9}, {10}={11}, 
                    {12}={13}, {14}={15}, {16}={17}, {18}={19}
                  WHERE {0}={1}",
                    DbDefines.EnclosuresTableColumns.LocalID, 
                    DbDefines.EnclosuresTableColumns.LocalIDParameter,
                    DbDefines.EnclosuresTableColumns.ParentID,
                    DbDefines.EnclosuresTableColumns.ParentIDParameter,   
                    DbDefines.EnclosuresTableColumns.Active,
                    DbDefines.EnclosuresTableColumns.ActiveParameter,                       
                    DbDefines.EnclosuresTableColumns.DownloadMimeType, 
                    DbDefines.EnclosuresTableColumns.DownloadMimeTypeParameter,                           
                    DbDefines.EnclosuresTableColumns.DownloadUrl, 
                    DbDefines.EnclosuresTableColumns.DownloadUrlParameter, 
                    DbDefines.EnclosuresTableColumns.LastDownloadError, 
                    DbDefines.EnclosuresTableColumns.LastDownloadErrorParameter,                            
                    DbDefines.EnclosuresTableColumns.Length, 
                    DbDefines.EnclosuresTableColumns.LengthParameter,                            
                    DbDefines.EnclosuresTableColumns.LocalPath, 
                    DbDefines.EnclosuresTableColumns.LocalPathParameter,                            
                    DbDefines.EnclosuresTableColumns.Type,
                    DbDefines.EnclosuresTableColumns.TypeParameter,                            
                    DbDefines.EnclosuresTableColumns.Url,  
                    DbDefines.EnclosuresTableColumns.UrlParameter                          
            );
        
        public static void Init ()
        {
            DatabaseManager.ExecuteNonQuery (initQuery); 
        }
        
        public static bool Commit (IEnumerable<FeedEnclosure> enclosures)
        {
            if (enclosures == null) {
                throw new ArgumentNullException ("enclosures");
            }    
            
            bool ret = false;
        
            try {            
                ICollection<FeedEnclosure> enclosureCol = 
                    enclosures as ICollection<FeedEnclosure>;
            
                Dictionary<QueuedDbCommand,FeedEnclosure> enclosureCommandPairs = 
                    (enclosureCol == null) ? new Dictionary<QueuedDbCommand,FeedEnclosure> () : 
                    new Dictionary<QueuedDbCommand,FeedEnclosure> (enclosureCol.Count);
                
                foreach (FeedEnclosure enc in enclosures) {
                    enclosureCommandPairs.Add (
                        QueuedDbCommand.CreateScalar (
                            EnclosuresTableManager.CreateInsertCommand (enc)
                        ), enc
                    );          
                }
                
                DatabaseManager.Enqueue (enclosureCommandPairs.Keys);
                
                foreach (KeyValuePair<QueuedDbCommand,FeedEnclosure> kvp in enclosureCommandPairs) {
                    kvp.Value.LocalID = Convert.ToInt64 (kvp.Key.ScalarResult);
                }  
            } catch {}
        
            return ret;
        }        
        
        public static IDbCommand CreateInsertCommand (FeedEnclosure enclosure)
        {
            if (enclosure == null) {
                throw new ArgumentNullException ("enclosure");
            }
            
            return CreateInsertCommandImpl (enclosure);
        }
        
        private static IDbCommand CreateInsertCommandImpl (FeedEnclosure enclosure)
        {
            return DatabaseManager.CreateCommand (
                insertIntoEnclosuresQuery,
                DbDefines.EnclosuresTableColumns.ParentID, 
                enclosure.Parent.LocalID.ToString (),
                DbDefines.EnclosuresTableColumns.Active,
                (enclosure.Active) ? "1" : "0",                                               
                DbDefines.EnclosuresTableColumns.DownloadMimeType, 
                enclosure.DownloadMimeType,
                DbDefines.EnclosuresTableColumns.DownloadUrl, 
                enclosure.DownloadUrl,
                DbDefines.EnclosuresTableColumns.LastDownloadError,
                ((int)enclosure.LastDownloadError).ToString (),
                DbDefines.EnclosuresTableColumns.Length, 
                enclosure.Length.ToString (),
                DbDefines.EnclosuresTableColumns.LocalPath, 
                enclosure.LocalPath,
                DbDefines.EnclosuresTableColumns.Type, 
                enclosure.Type,
                DbDefines.EnclosuresTableColumns.Url, 
                enclosure.Url 
            );
        }
        
        public static long Insert (FeedEnclosure enclosure)
        {
            if (enclosure == null) {
                throw new ArgumentNullException ("enclosure");
            }
            
            long ret = -1;
            
            if (enclosure.Parent == null) {
                return ret;
            }            
            
            ret = Convert.ToInt64 (
                DatabaseManager.ExecuteScalar (
                    CreateInsertCommandImpl (enclosure)
                )
            );             
            
            return ret;
        }
        
        public static bool Insert (IEnumerable<FeedEnclosure> enclosures)
        {
            if (enclosures == null) {
                throw new ArgumentNullException ("enclosures");
            }    
            
            bool ret = false;
        
            try {
                foreach (FeedEnclosure e in enclosures) {                
                    Insert (e);
                }
                
                ret = true;
            } catch {}     
            
            return ret;
        }           

        public static void Update (FeedEnclosure enclosure)
        {
            if (enclosure == null) {
                throw new ArgumentNullException ("enclosure");       
            }   

            DatabaseManager.ExecuteNonQuery (
                updateEnclosuresQuery, 
                DbDefines.EnclosuresTableColumns.LocalID, 
                enclosure.LocalID.ToString (),
                DbDefines.EnclosuresTableColumns.ParentID, 
                (enclosure.Parent != null) ? enclosure.Parent.LocalID.ToString () : "-1",
                DbDefines.EnclosuresTableColumns.Active,
                (enclosure.Active) ? "1" : "0",                                                                                            
                DbDefines.EnclosuresTableColumns.DownloadMimeType, 
                enclosure.DownloadMimeType,
                DbDefines.EnclosuresTableColumns.DownloadUrl, 
                enclosure.DownloadUrl,
                DbDefines.EnclosuresTableColumns.LastDownloadError,
                ((int)enclosure.LastDownloadError).ToString (),
                DbDefines.EnclosuresTableColumns.Length, 
                enclosure.Length.ToString (),
                DbDefines.EnclosuresTableColumns.LocalPath, 
                enclosure.LocalPath,
                DbDefines.EnclosuresTableColumns.Type, 
                enclosure.Type,
                DbDefines.EnclosuresTableColumns.Url, 
                enclosure.Url
            );
        }    
        
        public static bool Update (IEnumerable<FeedEnclosure> enclosures)
        {
            if (enclosures == null) {
                throw new ArgumentNullException ("enclosures");
            }    
            
            bool ret = false;
        
            try {
                foreach (FeedEnclosure e in enclosures) {                
                    Update (e);
                }
                
                ret = true;
            } catch {}   
            
            return ret;
        }           
    }
}
