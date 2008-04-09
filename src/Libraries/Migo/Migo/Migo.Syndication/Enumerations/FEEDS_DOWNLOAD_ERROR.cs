/*************************************************************************** 
 *  FEEDS_DOWNLOAD_ERROR.cs
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
 
 namespace Migo.Syndication 
 {
    public enum FEEDS_DOWNLOAD_ERROR : int
    {
        FDE_NONE = 0,
        FDE_DOWNLOAD_FAILED = 1,
        FDE_INVALID_FEED_FORMAT = 2,
        FDE_NORMALIZATION_FAILED = 3,
        FDE_PERSISTENCE_FAILED = 4,
        FDE_DOWNLOAD_BLOCKED = 5,
        FDE_CANCELED = 6,
        FDE_UNSUPPORTED_AUTH = 7,
        FDE_BACKGROUND_DOWNLOAD_DISABLED = 8,
        FDE_NOT_EXIST = 9,
        FDE_UNSUPPORTED_MSXML = 10,
        FDE_UNSUPPORTED_DTD = 11,
        FDE_DOWNLOAD_SIZE_LIMIT_EXCEEDED = 12
    }
 }
