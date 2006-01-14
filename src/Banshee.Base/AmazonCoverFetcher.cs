/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  AmazonCoverFetcher.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Web;
using System.Net;

public class AmazonCoverFetcher
{
    private static readonly string AmazonImageUri = 
        "http://images.amazon.com/images/P/{0}.01._SCLZZZZZZZ_.jpg";
    
    public static bool Fetch(string asin, string saveDirectory)
    {
        if(asin == null || saveDirectory == null) {
            return false;
        }
        
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
            String.Format(AmazonImageUri, asin));
        
        request.Timeout = 10000;
        
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        
        foreach(string content_type in response.Headers.GetValues("Content-Type")) {
            if(content_type == "image/gif") {
                return true;
            }
        }
        
        if(response.ContentLength < 0) {
            return false;
        }
        
        BinaryReader reader = new BinaryReader(response.GetResponseStream());
    
        byte [] image_bytes = reader.ReadBytes((int)response.ContentLength);
        
        FileStream stream = new FileStream(saveDirectory + Path.DirectorySeparatorChar 
            + asin + ".jpg", FileMode.Create);
        stream.Write(image_bytes, 0, image_bytes.Length);
        stream.Close();
        reader.Close();
        
        return true;
    }
}
