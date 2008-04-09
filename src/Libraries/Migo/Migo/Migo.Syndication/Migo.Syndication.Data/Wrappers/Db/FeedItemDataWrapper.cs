/*************************************************************************** 
 *  FeedItemDataWrapper.cs
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

namespace Migo.Syndication.Data
{    
    enum FeedItemPropertyID : int
    {
        Min = LocalID,        
        LocalID = 0,
        ParentID,  
        Active,
        Author,
        Comments,
        Description,
        Guid,
        IsRead,
        LastDownloadTime,
        Link,
        Modified,
        PubDate,
        Title,
        Max = Title
    }
   
    class FeedItemDataWrapper : DataWrapper, IFeedItemWrapper
    {
        public string Author 
        { 
            get { return GetStringSafe ((int) FeedItemPropertyID.Author); } 
        }
        
        public bool Active 
        { 
            get { return GetBooleanSafe ((int) FeedItemPropertyID.Active); } 
        }        
        
        public string Comments 
        { 
            get { return GetStringSafe ((int) FeedItemPropertyID.Comments); } 
        }
        
        public string Description 
        { 
            get { return GetStringSafe ((int) FeedItemPropertyID.Description); } 
        }

        public IFeedEnclosureWrapper Enclosure
        {
            get { return null; }
        }
        
        public string Guid 
        { 
            get { return GetStringSafe ((int) FeedItemPropertyID.Guid); } 
        }
        
        public bool IsRead 
        { 
            get { return GetBooleanSafe ((int) FeedItemPropertyID.IsRead); } 
        }
        
        public DateTime LastDownloadTime 
        { 
            get {
                return GetDateTimeSafe ((int) FeedItemPropertyID.LastDownloadTime); 
            } 
        }  
        
        public string Link 
        { 
            get { return GetStringSafe ((int) FeedItemPropertyID.Link); } 
        }
        
        public long LocalID 
        { 
            get { return GetInt64Safe ((int) FeedItemPropertyID.LocalID); } 
        }
        
        public DateTime Modified 
        { 
            get { return GetDateTimeSafe ((int) FeedItemPropertyID.Modified); } 
        }      

        public long ParentID 
        { 
            get { return GetInt64Safe ((int) FeedItemPropertyID.ParentID); } 
        }        
        
        public DateTime PubDate 
        { 
            get { return GetDateTimeSafe ((int) FeedItemPropertyID.PubDate); } 
        }       
        
        public string Title 
        { 
            get { return GetStringSafe ((int) FeedItemPropertyID.Title); } 
        }         
        
        public FeedItemDataWrapper (IDataReader reader) : base (reader)
        {
            Console.WriteLine(reader.FieldCount);
            Console.WriteLine((int)FeedItemPropertyID.Max+1);
            
            if (reader.FieldCount != (int)FeedItemPropertyID.Max+1) {
                throw new ArgumentException (
                    "reader does not appear to be associated with the items table"
                );
            }
        }
    }
}
