/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  SimpleDisc.cs
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
using System.Collections;

namespace MusicBrainz
{
    public class SimpleDisc : IEnumerable, IDisposable
    {
        private Client client;
        private SimpleTrack [] tracks;
        private int [] lengths;
        private string artist_name;
        private string album_name;
        private string cover_art_url;
        private string amazon_asin;
        private DateTime release_date = DateTime.MinValue;
        
        public SimpleDisc(string device, Client client)
        {
            this.client = client;
            client.Device = device;
           
            ReadCDToc();
            
            tracks = new SimpleTrack[lengths.Length];
            
            for(int i = 0; i < lengths.Length; i++) {
                tracks[i] = new SimpleTrack(i + 1, lengths[i]);
            }
        }
        
        public SimpleDisc(string device) : this(device, new Client())
        {
        
        }
        
        public void Dispose()
        {
            client.Dispose();
        }
        
        private static int SectorsToSeconds(int sectors)
        {
            return sectors * 2352 / (44100 / 8) / 16 / 2;
        }
        
        private void ReadCDToc()
        {
            client.Query(Rdf.MBQ_GetCDTOC);
            int track_count = client.GetResultInt(Rdf.MBE_TOCGetLastTrack);
            
            if(track_count <= 0) {
                throw new ApplicationException("Invalid track count from TOC");
            }
            
            lengths = new int[track_count];
            
            for(int i = 1; i <= lengths.Length; i++) {
                lengths[i - 1] = SectorsToSeconds(client.GetResultInt(
                    Rdf.MBE_TOCGetTrackNumSectors, i + 1));
            }   
        }
        
        public void QueryCDMetadata()
        {
            if(!client.Query(Rdf.MBQ_GetCDInfo)) {
                throw new ApplicationException("Could not query CD");
            }
            
            int disc_count = client.GetResultInt(Rdf.MBE_GetNumAlbums);
            
            if(disc_count <= 0) {
                throw new ApplicationException("No Discs Found");
            }
            
            // handle more than 1 disc? not sure how this works? for multi-disc sets? 
            // if that's the case, we only care about the first one I guess... I hope
            
            if(!client.Select(Rdf.MBS_SelectAlbum, 1)) {
                throw new ApplicationException("Could not select disc");
            }
            
            amazon_asin = client.GetResultData(Rdf.MBE_AlbumGetAmazonAsin, 1);
            cover_art_url = client.GetResultData(Rdf.MBE_AlbumGetAmazonCoverartURL, 1);
            artist_name = client.GetResultData(Rdf.MBE_AlbumGetArtistName, 1);
            album_name = client.GetResultData(Rdf.MBE_AlbumGetAlbumName, 1);
            
            int track_count = client.GetResultInt(Rdf.MBE_AlbumGetNumTracks, 1);
            
            if(track_count <= 0) {
                throw new ApplicationException("Invalid track count from album query");
            }
                        
            tracks = new SimpleTrack[track_count];
            
            for(int i = 1; i <= tracks.Length; i++) {
                client.Select(Rdf.MBS_SelectTrack, i);
                
                tracks[i - 1] = new SimpleTrack(i, lengths[i - 1]);
                tracks[i - 1].Artist = client.GetResultData(Rdf.MBE_TrackGetArtistName);
                tracks[i - 1].Title = client.GetResultData(Rdf.MBE_TrackGetTrackName);
                
                int new_length = client.GetResultInt(Rdf.MBE_TrackGetTrackDuration);
                if(new_length > 0) {
                    tracks[i - 1].Length = new_length / 1000;
                }
                
                client.Select(Rdf.MBS_Back);
            }
            
            int num_releases = client.GetResultInt(Rdf.MBE_AlbumGetNumReleaseDates);
            
            if(num_releases > 0) {
                client.Select(Rdf.MBS_SelectReleaseDate, 1);
               
                string raw_date = client.GetResultData(Rdf.MBE_ReleaseGetDate);
                if(raw_date != null && raw_date != String.Empty) {
                    string [] parts = raw_date.Split('-');
                    if(parts.Length == 3) {
                        int year = Convert.ToInt32(parts[0]);
                        int month = Convert.ToInt32(parts[1]);
                        int day = Convert.ToInt32(parts[2]);
                        
                        release_date = new DateTime(year, month, day);
                    } 
                }
                
                client.Select(Rdf.MBS_Back);
            }
            
            client.Select(Rdf.MBS_Back);
        }
        
        public IEnumerator GetEnumerator()
        {
            foreach(SimpleTrack track in Tracks) {
                yield return track;
            }
        }
        
        public SimpleTrack [] Tracks {
            get {
                return tracks;
            }
        }
        
        public string CoverArtUrl {
            get {
                return cover_art_url;
            }
        }
        
        public string AmazonAsin {
            get {
                return amazon_asin;
            }
        }
        
        public string AlbumName {
            get {
                return album_name;
            }
        }
        
        public string ArtistName {
            get {
                return artist_name;
            }
        } 
        
        public DateTime ReleaseDate {
            get {
                return release_date;
            }
        }
        
        public Client Client {
            get {
                return client;
            }
        }
    }
}
