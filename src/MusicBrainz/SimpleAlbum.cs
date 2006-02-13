
/***************************************************************************
 *  SimpleAlbum.cs
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

namespace MusicBrainz
{
    public class SimpleAlbum
    {
        private string id;
        private string title;
        private string asin;
        private string cover_art_url;
        private bool various_artists;
        private DateTime release_date;
        
        private SimpleArtist artist;
        private SimpleTrack [] tracks;
        
        public SimpleAlbum(Client client, string id)
        {
            client.Depth = 4;
            id = client.GetID(id);
            
            if(!client.Query(Rdf.MBQ_GetAlbumById, id) || !client.Select(Rdf.MBS_SelectAlbum, 1)) {
                throw new ApplicationException("Could not fetch album with ID: " + id);    
            }
              
            this.id = client.GetID(client.GetResultData(Rdf.MBE_AlbumGetAlbumId));
            asin = client.GetResultData(Rdf.MBE_AlbumGetAmazonAsin);
            cover_art_url = client.GetResultData(Rdf.MBE_AlbumGetAmazonCoverartURL);
            string artist_name = client.GetResultData(Rdf.MBE_AlbumGetArtistName);
            Console.WriteLine(artist_name);
            title = client.GetResultData(Rdf.MBE_AlbumGetAlbumName);
            
            int track_count = client.GetResultInt(Rdf.MBE_AlbumGetNumTracks, 1);
            
            if(track_count <= 0) {
                throw new ApplicationException("Invalid track count from album query");
            }
                        
            tracks = new SimpleTrack[track_count];
            
            for(int i = 1; i <= tracks.Length; i++) {
                client.Select(Rdf.MBS_SelectTrack, i);
                
                tracks[i - 1] = new SimpleTrack(i, 0);
                tracks[i - 1].Artist = client.GetResultData(Rdf.MBE_TrackGetArtistName);
                tracks[i - 1].Title = client.GetResultData(Rdf.MBE_TrackGetTrackName);
                
                int length = client.GetResultInt(Rdf.MBE_TrackGetTrackDuration);
                tracks[i - 1].Length = length / 1000;
                
                client.Select(Rdf.MBS_Back);
            }
           
            if(client.GetResultInt(Rdf.MBE_AlbumGetNumReleaseDates) > 0) {
                client.Select(Rdf.MBS_SelectReleaseDate, 1);
                release_date = Utilities.StringToDateTime(
                    client.GetResultData(Rdf.MBE_ReleaseGetDate));
                client.Select(Rdf.MBS_Back);
            }
            
            client.Select(Rdf.MBS_Back);
        }
        
        public override string ToString()
        {
            string ret = String.Empty;
            
            ret += "ID:              " + ID + "\n";
            ret += "Album Title:     " + Title + "\n";
            ret += "Amazon ASIN:     " + Asin + "\n";
            ret += "Cover Art:       " + CoverArtUrl + "\n";
            ret += "Various Artists: " + VariousArtists + "\n";
            ret += "Release Date:    " + ReleaseDate + "\n";
            
            ret += "Tracks:\n";
            
            foreach(SimpleTrack track in tracks) {
                ret += track + "\n";
            }
            
            return ret;
        }
        
        public string ID {
            get {
                return id;
            }
        }
        
        public string Title {
            get {
                return title;
            }
        }
        
        public SimpleArtist Artist {
            get {
                return artist;
            }
        }
        
        public string Asin {
            get {
                return asin;
            }
        }
        
        public string CoverArtUrl {
            get {
                return cover_art_url;
            }
        }
        
        public bool VariousArtists {
            get {
                return various_artists;
            }
        }
        
        public DateTime ReleaseDate {
            get {
                return release_date;
            }
        }
        
        public SimpleTrack [] Tracks {
            get {
                return tracks;
            }
        } 
    }
}
