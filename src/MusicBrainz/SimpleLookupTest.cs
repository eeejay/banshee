/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  SimpleLookupTest.cs
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

public class SimpleLookupTest
{
    private static void Main(string [] args)
    {
        string artist = null;
        string album = null;
        string title = null;
        
        for(int i = 0; i < args.Length; i++) {
            switch(args[i]) {
                case "-artist":
                    artist = args[++i];
                    break;
                case "-album":
                    album = args[++i];
                    break;
                case "-title":
                    title = args[++i];
                    break;
            }
        }
    
        Console.WriteLine("Querying MusicBrainz with FileLookup request:");
        Console.WriteLine("   Artist [{0}]", artist);
        Console.WriteLine("   Album  [{0}]", album);
        Console.WriteLine("   Track  [{0}]", title);
        Console.WriteLine("---------------------------------------------");
    
        using(Client client = new Client()) {
            SimpleTrack track = SimpleQuery.FileLookup(client, artist, album, title, 0, 0);
            Console.WriteLine("Artist: {0}", track.Artist);
            Console.WriteLine("Album: {0}", track.Album);
            Console.WriteLine("Track: {0}", track.Title);
            Console.WriteLine("Duration: {0}", track.Length);
            Console.WriteLine("Number: {0}", track.TrackNumber);
            Console.WriteLine("Count: {0}", track.TrackCount);
            Console.WriteLine("ASIN: {0}", track.Asin);
        }
    }
}
