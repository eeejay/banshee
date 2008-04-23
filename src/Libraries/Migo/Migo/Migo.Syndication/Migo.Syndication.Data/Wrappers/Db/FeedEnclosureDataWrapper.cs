/*************************************************************************** 
 *  FeedEnclosureDataWrapper.cs
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
    enum FeedEnclosurePropertyID : int
    {
        Min = LocalID,        
        LocalID = 0,
        ParentID,  
        Active,
        DownloadMimeType,
        DownloadUrl,
        LastDownloadError,
        Length,
        LocalPath,
        Type,
        Url,
        Max = Url
    }
   
    public class FeedEnclosureDataWrapper : DataWrapper, IFeedEnclosureWrapper
    {
        public bool Active 
        {
            get {
                return GetBooleanSafe (
                    (int) FeedEnclosurePropertyID.Active
                );
            }            
        }
        
        public string DownloadMimeType 
        { 
            get {
                return GetStringSafe (
                    (int) FeedEnclosurePropertyID.DownloadMimeType
                );
            } 
        }
        
        public string DownloadUrl 
        { 
            get {
                return GetStringSafe (
                    (int) FeedEnclosurePropertyID.DownloadUrl
                );
            } 
        }        

        public FeedDownloadError LastDownloadError { 
            get { 
                return (FeedDownloadError) GetInt64Safe (
                    (int) FeedEnclosurePropertyID.LastDownloadError
                );            
            }
        }   
                
        public long Length 
        { 
            get {
                return GetInt64Safe ((int) FeedEnclosurePropertyID.Length); 
            } 
        }

        public long LocalID 
        { 
            get { 
                return GetInt64Safe ((int)FeedEnclosurePropertyID.LocalID);
            } 
        }

        public long ParentID 
        { 
            get {
                return GetInt64Safe ((int) FeedEnclosurePropertyID.ParentID); 
            }
        }

        public string LocalPath 
        { 
            get {
                return GetStringSafe ((int) FeedEnclosurePropertyID.LocalPath); 
            } 
        }
        
        public string Type 
        { 
            get { return GetStringSafe ((int) FeedEnclosurePropertyID.Type); } 
        }        
        
        public string Url 
        { 
            get { return GetStringSafe ((int) FeedEnclosurePropertyID.Url); } 
        }   
        
        public FeedEnclosureDataWrapper (IDataReader reader) : base (reader)
        {
            Console.WriteLine(reader.FieldCount);
            Console.WriteLine((int)FeedEnclosurePropertyID.Max+1);
            if (reader.FieldCount != (int)FeedEnclosurePropertyID.Max+1) {
                throw new ArgumentException (
                    "reader does not appear to be associated with the enclosures table"
                );
            }
        }
    }
}
