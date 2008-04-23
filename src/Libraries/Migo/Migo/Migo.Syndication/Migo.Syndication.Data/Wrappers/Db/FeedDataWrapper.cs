/*************************************************************************** 
 *  FeedDataWrapper.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
 ****************************************************************************/
 
/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted,free of charge,to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"), 
 *  to deal in the Software without restriction,including without limitation  
 *  the rights to use,copy,modify,merge,publish,distribute,sublicense, 
 *  and/or sell copies of the Software,and to permit persons to whom the  
 *  Software is furnished to do so,subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS",WITHOUT WARRANTY OF ANY KIND,EXPRESS OR 
 *  IMPLIED,INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,DAMAGES OR OTHER 
 *  LIABILITY,WHETHER IN AN ACTION OF CONTRACT,TORT OR OTHERWISE,ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Data;
using System.Collections.Generic;

namespace Migo.Syndication.Data
{    
    enum FeedPropertyID : int
    {
        Min = LocalID,
        LocalID = 0,        
        Copyright,
        Description,
        DownloadEnclosuresAutomatically,
        DownloadUrl,
        Image,
        Interval,
        IsList,
        Language,
        LastBuildDate,
        LastDownloadError,
        LastDownloadTime,
        LastWriteTime,
        Link,
        LocalEnclosurePath,
        MaxItemCount,
        Name,
        PubDate,
        SyncSetting,
        Title,
        Ttl,
        Url,
        Max = Url
    }
   
    class FeedDataWrapper : DataWrapper, IFeedWrapper
    {
        public string Copyright 
        { 
            get { return GetStringSafe ((int)FeedPropertyID.Copyright); } 
        }
        
        public string Description 
        { 
            get { return GetStringSafe ((int)FeedPropertyID.Description); } 
        }
        
        public bool DownloadEnclosuresAutomatically 
        { 
            get { 
                return GetBooleanSafe (
                    (int)FeedPropertyID.DownloadEnclosuresAutomatically
                );
            } 
        }
        
        public string DownloadUrl 
        { 
            get { return GetStringSafe ((int)FeedPropertyID.DownloadUrl); } 
        }     
        
        public string Image 
        { 
            get { return GetStringSafe ((int)FeedPropertyID.Image); } 
        }

        public long Interval        
        { 
            get { return GetInt32Safe ((int)FeedPropertyID.Interval); } 
        }

        public bool IsList 
        { 
            get { return GetBooleanSafe ((int)FeedPropertyID.IsList); } 
        }
        
        public IEnumerable<IFeedItemWrapper> Items 
        {
            get { return null; }   
        }
        
        public string Language 
        { 
            get { return GetStringSafe ((int)FeedPropertyID.Language); } 
        }
        
        public DateTime LastBuildDate 
        { 
            get { return GetDateTimeSafe ((int)FeedPropertyID.LastBuildDate); } 
        }
        
        public FeedDownloadError LastDownloadError 
        { 
            get { 
                int dbVal = GetInt32Safe ((int)FeedPropertyID.LastDownloadError);

                if (!Enum.IsDefined (typeof (FeedDownloadError), dbVal)) {
                    dbVal = 0;
                }                 
                
                return (FeedDownloadError) dbVal;
            } 
        }
        
        public DateTime LastDownloadTime 
        { 
            get { 
                return GetDateTimeSafe ((int)FeedPropertyID.LastDownloadTime);
            } 
        }
        
        public DateTime LastWriteTime 
        { 
            get { 
                return GetDateTimeSafe ((int)FeedPropertyID.LastWriteTime);
            } 
        }
        
        public string Link 
        { 
            get { return GetStringSafe ((int)FeedPropertyID.Link); } 
        }
        
        public string LocalEnclosurePath 
        { 
            get { 
                return GetStringSafe ((int)FeedPropertyID.LocalEnclosurePath); 
            }
        }
        
        public long LocalID 
        { 
            get { return GetInt64Safe ((int)FeedPropertyID.LocalID); } 
        }

        public long MaxItemCount
        { 
            get { 
                CheckReader (); 
                return GetInt64Safe ((int)FeedPropertyID.MaxItemCount);
            } 
        }
            
        public string Name 
        { 
            get { return GetStringSafe ((int)FeedPropertyID.Name); } 
        }
        
        public DateTime PubDate 
        { 
            get { return GetDateTimeSafe ((int)FeedPropertyID.PubDate); } 
        }
        
        public FEEDS_SYNC_SETTING SyncSetting { 
            get {                 
                int dbVal = GetInt32Safe ((int)FeedPropertyID.SyncSetting);

                if (!Enum.IsDefined (typeof (FEEDS_SYNC_SETTING), dbVal)) {
                    dbVal = 0;
                }                

                return (FEEDS_SYNC_SETTING) dbVal;
            } 
        }
        
        public string Title 
        { 
            get { return GetStringSafe ((int)FeedPropertyID.Title); } 
        }
                
        public long Ttl 
        { 
            get { return GetInt32Safe ((int)FeedPropertyID.Ttl); } 
        }  
        
        public string Url 
        { 
            get { return GetStringSafe ((int)FeedPropertyID.Url); } 
        }        
        
        public FeedDataWrapper (IDataReader Reader) : base (Reader)
        {
            if (Reader.FieldCount != (int)FeedPropertyID.Max+1) {
                Reader = null;
                        
                throw new ArgumentException (
                    "Reader does not appear to be associated with the feeds table"
                );
            }
        }
    }
}
