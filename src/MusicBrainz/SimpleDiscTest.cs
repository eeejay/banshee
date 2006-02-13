
/***************************************************************************
 *  SimpleDiscTest.cs
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
using MusicBrainz;

public class SimpleDiscTest
{
    private static void Main()
    {
        using(SimpleDisc disc = new SimpleDisc("/dev/hdc")) {
           
            // Actually ask the MB server for metadata. As soon as a SimpleDisc is instantiated,
            // a disc layout is created based on reading the CD TOC directly. This query updates
            // that layout. For applications where UI must be responsive, run this query in
            // a thread.
            
            // To set proxy server information, call disc.Client.SetProxy(server, port) before
            // calling this query
            
            disc.QueryCDMetadata();
        
            Console.WriteLine("Artist Name   : " + disc.ArtistName);
            Console.WriteLine("Album Name    : " + disc.AlbumName);
            Console.WriteLine("Cover Art URL : " + disc.CoverArtUrl);
            Console.WriteLine("Amazon ASIN   : " + disc.AmazonAsin);
            Console.WriteLine("Release Date  : " + disc.ReleaseDate);
            Console.WriteLine("");
            
            foreach(SimpleTrack track in disc) {
                Console.WriteLine("{0:00}: [{1} - {2}] {3}:{4:00} ({5})", 
                    track.Index, track.Artist, track.Title, 
                    track.Minutes, track.Seconds, track.Length);
            }
        }
    }
}
