
/***************************************************************************
 *  SimpleQuery.cs
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
    public static class SimpleQuery
    {
        public static SimpleTrack FileLookup(Client client, string artistName, string albumName, 
            string trackName, int trackNum, int duration)
        {
            client.Depth = 4;
            
            if(!client.Query(Rdf.MBQ_FileInfoLookup,
                String.Empty, // trmid
                artistName,
                albumName,
                trackName,
                trackNum.ToString(),
                duration.ToString(),
                String.Empty, // filename
                String.Empty, // artistid
                String.Empty, // albumid
                String.Empty)) { // trackid
                throw new ApplicationException("File Lookup Query unsuccessful");
            }
            
            client.Select(Rdf.MBS_Rewind);
            
            if(!client.Select(Rdf.MBS_SelectLookupResult, 1)) {
                throw new ApplicationException("Selection failed");
            }
            
            SimpleTrack track = new SimpleTrack();
            string result_type = client.GetID(client.GetResultData(Rdf.MBE_LookupGetType));
            
            switch(result_type) {
                case "AlbumTrackResult":
                    client.Select(Rdf.MBS_SelectLookupResultTrack);
                    track.Title = client.GetResultData(Rdf.MBE_TrackGetTrackName);
                    track.Artist = client.GetResultData(Rdf.MBE_TrackGetArtistName);
                    track.Length = client.GetResultInt(Rdf.MBE_TrackGetTrackDuration);
                    client.Select(Rdf.MBS_Back);
                    
                    client.Select(Rdf.MBS_SelectLookupResultAlbum, 1);
                    track.Album = client.GetResultData(Rdf.MBE_AlbumGetAlbumName);
                    track.TrackCount = client.GetResultInt(Rdf.MBE_AlbumGetNumTracks);
                    track.TrackNumber = client.GetResultInt(Rdf.MBE_AlbumGetTrackNum);
                    track.Asin = client.GetResultData(Rdf.MBE_AlbumGetAmazonAsin);
                    client.Select(Rdf.MBS_Back);
                    break;
                case "AlbumResult":
                    client.Select(Rdf.MBS_SelectLookupResultAlbum, 1);
                    track.TrackCount = client.GetResultInt(Rdf.MBE_AlbumGetNumTracks);
                    track.Album = client.GetResultData(Rdf.MBE_AlbumGetAlbumName);
                    track.Asin = client.GetResultData(Rdf.MBE_AlbumGetAmazonAsin);
                    
                    string track_id = client.GetResultData(Rdf.MBE_AlbumGetTrackId, trackNum);
                    
                    if(client.Query(Rdf.MBQ_GetTrackById, client.GetID(track_id))) {
                        client.Select(Rdf.MBS_SelectTrack, 1);
                        track.Title = client.GetResultData(Rdf.MBE_TrackGetTrackName);
                        track.Artist = client.GetResultData(Rdf.MBE_TrackGetArtistName);
                        track.TrackNumber = client.GetResultInt(Rdf.MBE_TrackGetTrackNum);
                        track.Length = client.GetResultInt(Rdf.MBE_TrackGetTrackDuration);
                        client.Select(Rdf.MBS_Back);
                    }
                    
                    client.Select(Rdf.MBS_Back);
                    break;
                default:
                    throw new ApplicationException("Invalid result type: " + result_type);
            }
            
            return track;
        }
    }
}
