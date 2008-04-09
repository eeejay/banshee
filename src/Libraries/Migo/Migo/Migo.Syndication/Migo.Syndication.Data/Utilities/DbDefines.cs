/*************************************************************************** 
 *  DbDefines.cs
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

namespace Migo.Syndication.Data
{
    static class DbDefines
    {
        public static string ParameterIdentifier 
        { 
            get { return SQLiteUtility.ParameterIdentifier; } 
        }
      
        public static string LastInsertIDQuery 
        { 
            get { return SQLiteUtility.LastInsertIDQuery; } 
        }
        
        public static class FeedsTableColumns
        {
            public const string LocalID = "local_id";        
            public const string Copyright = "copyright";  
            public const string Description = "description";  
            public const string DownloadEnclosuresAutomatically = "download_enclosures_automatically";  
            public const string DownloadUrl = "download_url";  
            public const string Image = "image";
            public const string Interval = "interval";  
            public const string IsList = "is_list";  
            public const string Language = "language";  
            public const string LastBuildDate = "last_build_date";  
            public const string LastDownloadError = "last_download_error";
            public const string LastDownloadTime = "last_download_time";  
            public const string LastWriteTime = "last_write_time";  
            public const string Link = "link";  
            public const string LocalEnclosurePath = "local_enclosure_path";  
            public const string MaxItemCount = "max_item_count";
            public const string Name = "name";  
            public const string PubDate = "pubdate";  
            public const string SyncSetting = "sync_setting";  
            public const string Title = "title";  
            public const string Ttl = "ttl";
            public const string Url = "url";              
            
            public static readonly string LocalIDParameter = DbDefines.ParameterIdentifier + LocalID;        
            public static readonly string CopyrightParameter = DbDefines.ParameterIdentifier + Copyright;  
            public static readonly string DescriptionParameter = DbDefines.ParameterIdentifier + Description;  
            public static readonly string DownloadEnclosuresAutomaticallyParameter = DbDefines.ParameterIdentifier + DownloadEnclosuresAutomatically;  
            public static readonly string DownloadUrlParameter = DbDefines.ParameterIdentifier + DownloadUrl;  
            public static readonly string ImageParameter = DbDefines.ParameterIdentifier + Image;
            public static readonly string IntervalParameter = DbDefines.ParameterIdentifier + Interval;  
            public static readonly string IsListParameter = DbDefines.ParameterIdentifier + IsList;  
            public static readonly string LanguageParameter = DbDefines.ParameterIdentifier + Language;  
            public static readonly string LastBuildDateParameter = DbDefines.ParameterIdentifier + LastBuildDate;  
            public static readonly string LastDownloadErrorParameter = DbDefines.ParameterIdentifier + LastDownloadError;
            public static readonly string LastDownloadTimeParameter = DbDefines.ParameterIdentifier + LastDownloadTime;  
            public static readonly string LastWriteTimeParameter = DbDefines.ParameterIdentifier + LastWriteTime;  
            public static readonly string LinkParameter = DbDefines.ParameterIdentifier + Link;  
            public static readonly string LocalEnclosurePathParameter = DbDefines.ParameterIdentifier + LocalEnclosurePath;  
            public static readonly string MaxItemCountParameter = DbDefines.ParameterIdentifier + MaxItemCount;
            public static readonly string NameParameter = DbDefines.ParameterIdentifier + Name;  
            public static readonly string PubDateParameter = DbDefines.ParameterIdentifier + PubDate;  
            public static readonly string SyncSettingParameter = DbDefines.ParameterIdentifier + SyncSetting;  
            public static readonly string TitleParameter = DbDefines.ParameterIdentifier + Title;  
            public static readonly string TtlParameter = DbDefines.ParameterIdentifier + Ttl;
            public static readonly string UrlParameter = DbDefines.ParameterIdentifier + Url;               
        }
        
        public static class ItemsTableColumns
        {
            public const string LocalID = "local_id";        
            public const string ParentID = "parent_id";  
            public const string Active = "active";              
            public const string Author = "author";  
            public const string Comments = "comments";  
            public const string Description = "description";
            public const string Guid = "guid";  
            public const string IsRead = "is_read";  
            public const string LastDownloadTime = "last_download_time";  
            public const string Link = "link";  
            public const string Modified = "modified";
            public const string PubDate = "pubdate";  
            public const string Title = "title"; 
            
            public static readonly string LocalIDParameter = DbDefines.ParameterIdentifier + LocalID;        
            public static readonly string ParentIDParameter = DbDefines.ParameterIdentifier + ParentID;  
            public static readonly string ActiveParameter = DbDefines.ParameterIdentifier + Active;  
            public static readonly string AuthorParameter = DbDefines.ParameterIdentifier + Author;  
            public static readonly string CommentsParameter = DbDefines.ParameterIdentifier + Comments;  
            public static readonly string DescriptionParameter = DbDefines.ParameterIdentifier + Description;
            public static readonly string GuidParameter = DbDefines.ParameterIdentifier + Guid;  
            public static readonly string IsReadParameter = DbDefines.ParameterIdentifier + IsRead;  
            public static readonly string LastDownloadTimeParameter = DbDefines.ParameterIdentifier + LastDownloadTime;  
            public static readonly string LinkParameter = DbDefines.ParameterIdentifier + Link;  
            public static readonly string ModifiedParameter = DbDefines.ParameterIdentifier + Modified;
            public static readonly string PubDateParameter = DbDefines.ParameterIdentifier + PubDate;  
            public static readonly string TitleParameter = DbDefines.ParameterIdentifier + Title;              
        }
        
        public static class EnclosuresTableColumns
        {
            public const string LocalID = "local_id";        
            public const string ParentID = "parent_id"; 
            public const string Active = "active";             
            public const string DownloadMimeType = "download_mime_type"; 
            public const string DownloadUrl = "download_url"; 
            public const string LastDownloadError = "last_download_error";
            public const string Length = "length";
            public const string LocalPath = "local_path";  
            public const string Type = "type";  
            public const string Url = "url";  

            public static readonly string LocalIDParameter = DbDefines.ParameterIdentifier + LocalID;        
            public static readonly string ParentIDParameter = DbDefines.ParameterIdentifier + ParentID;  
            public static readonly string ActiveParameter = DbDefines.ParameterIdentifier + Active;              
            public static readonly string DownloadMimeTypeParameter = DbDefines.ParameterIdentifier + DownloadMimeType; 
            public static readonly string DownloadUrlParameter = DbDefines.ParameterIdentifier + DownloadUrl;  
            public static readonly string LastDownloadErrorParameter = DbDefines.ParameterIdentifier + LastDownloadError;
            public static readonly string LengthParameter = DbDefines.ParameterIdentifier + Length;
            public static readonly string LocalPathParameter = DbDefines.ParameterIdentifier + LocalPath;  
            public static readonly string TypeParameter = DbDefines.ParameterIdentifier + Type;  
            public static readonly string UrlParameter = DbDefines.ParameterIdentifier + Url;                
        }         
    }
}
