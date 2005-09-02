/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  FileNamePattern.cs
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
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;

namespace Banshee
{
    public class FileNamePattern
    {
        public static string CreateFromTrackInfo(TrackInfo track)
        {
            string pattern;

            try {
                pattern = Core.GconfClient.Get(GConfKeys.FileNamePattern) 
                    as string;
            } catch(Exception) {
                pattern = null;
            }
            
            return CreateFromTrackInfo(pattern, track);
        }

        public static string CreateFromTrackInfo(string pattern, 
            TrackInfo track)
        {
            Hashtable convtable = new Hashtable();
            string repl_pattern;

            if(pattern == null || pattern.Trim() == String.Empty)
                repl_pattern = "%artist%/%album%/%track_number%. %title%";
            else
                repl_pattern = pattern;

            convtable["%artist%"] = Escape(track.DisplayArtist);
            convtable["%album%"] = Escape(track.DisplayAlbum);
            convtable["%title%"] = Escape(track.DisplayTitle);

            convtable["%track_count%"] = String.Format("{0:00}", 
                track.TrackCount);
            convtable["%track_number%"] = String.Format("{0:00}", 
                track.TrackNumber);
            convtable["%track_count_nz%"] = 
                String.Format("{0}", track.TrackCount);
            convtable["%track_number_nz%"] = 
                String.Format("{0}", track.TrackNumber);

            foreach(string key in convtable.Keys)
                repl_pattern = repl_pattern.Replace(key, convtable[key] 
                    as string);

            return repl_pattern;
        }

        public static string BuildFull(TrackInfo track, string ext)
        {
            string songpath = CreateFromTrackInfo(track) + "." + ext;
            string dir = Path.GetFullPath(Core.Library.Location + 
                Path.DirectorySeparatorChar + 
                Path.GetDirectoryName(songpath));
            string filename = dir + Path.DirectorySeparatorChar + 
                Path.GetFileName(songpath);
             
            if(!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
             
            return filename;
        }

        public static string Escape(string input)
        {
            return Regex.Replace(input, @"[\\/\$\%\?\*]+", "_");
        }
    }
}
