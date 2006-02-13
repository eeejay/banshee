
/***************************************************************************
 *  Rdf.cs
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
 
namespace MusicBrainz
{
    public sealed class Rdf
    {
        public static readonly string
            MBI_VARIOUS_ARTIST_ID =
             "89ad4ac3-39f7-470e-963a-56509c546377",
            MBS_Rewind =
             "[REWIND]",
            MBS_Back =
             "[BACK]",
            MBS_SelectArtist =
             "http://musicbrainz.org/mm/mm-2.1#artistList []",
            MBS_SelectAlbum =
             "http://musicbrainz.org/mm/mm-2.1#albumList []",
            MBS_SelectTrack =
             "http://musicbrainz.org/mm/mm-2.1#trackList []",
            MBS_SelectTrackArtist =
             "http://purl.org/dc/elements/1.1/creator",
            MBS_SelectTrackAlbum =
             "http://musicbrainz.org/mm/mq-1.1#album",
            MBS_SelectTrmid =
             "http://musicbrainz.org/mm/mm-2.1#trmidList []",
            MBS_SelectCdindexid =
             "http://musicbrainz.org/mm/mm-2.1#cdindexidList []",
            MBS_SelectReleaseDate =
             "http://musicbrainz.org/mm/mm-2.1#releaseDateList []",
            MBS_SelectLookupResult =
             "http://musicbrainz.org/mm/mq-1.1#lookupResultList []",
            MBS_SelectLookupResultArtist =
             "http://musicbrainz.org/mm/mq-1.1#artist",
            MBS_SelectLookupResultAlbum =
             "http://musicbrainz.org/mm/mq-1.1#album",
            MBS_SelectLookupResultTrack =
             "http://musicbrainz.org/mm/mq-1.1#track",
            MBE_GetStatus =
             "http://musicbrainz.org/mm/mq-1.1#status",
            MBE_GetNumArtists =
             "http://musicbrainz.org/mm/mm-2.1#artistList [COUNT]",
            MBE_GetNumAlbums =
             "http://musicbrainz.org/mm/mm-2.1#albumList [COUNT]",
            MBE_GetNumTracks =
             "http://musicbrainz.org/mm/mm-2.1#trackList [COUNT]",
            MBE_GetNumTrmids =
             "http://musicbrainz.org/mm/mm-2.1#trmidList [COUNT]",
            MBE_GetNumLookupResults =
             "http://musicbrainz.org/mm/mm-2.1#lookupResultList [COUNT]",
            MBE_ArtistGetArtistName =
             "http://purl.org/dc/elements/1.1/title",
            MBE_ArtistGetArtistSortName =
             "http://musicbrainz.org/mm/mm-2.1#sortName",
            MBE_ArtistGetArtistId =
             "",
            MBE_ArtistGetAlbumName =
             "http://musicbrainz.org/mm/mm-2.1#albumList [] http://purl.org/dc/elements/1.1/title",
            MBE_ArtistGetAlbumId =
             "http://musicbrainz.org/mm/mm-2.1#albumList []",
            MBE_AlbumGetAlbumName =
             "http://purl.org/dc/elements/1.1/title",
            MBE_AlbumGetAlbumId =
             "",
            MBE_AlbumGetAlbumStatus =
             "http://musicbrainz.org/mm/mm-2.1#releaseStatus",
            MBE_AlbumGetAlbumType =
             "http://musicbrainz.org/mm/mm-2.1#releaseType",
            MBE_AlbumGetAmazonAsin =
             "http://www.amazon.com/gp/aws/landing.html#Asin",
            MBE_AlbumGetAmazonCoverartURL =
             "http://musicbrainz.org/mm/mm-2.1#coverart",
            MBE_AlbumGetNumCdindexIds =
             "http://musicbrainz.org/mm/mm-2.1#cdindexidList [COUNT]",
            MBE_AlbumGetNumReleaseDates =
             "http://musicbrainz.org/mm/mm-2.1#releaseDateList [COUNT]",
            MBE_AlbumGetAlbumArtistId =
             "http://purl.org/dc/elements/1.1/creator",
            MBE_AlbumGetNumTracks =
             "http://musicbrainz.org/mm/mm-2.1#trackList [COUNT]",
            MBE_AlbumGetTrackId =
             "http://musicbrainz.org/mm/mm-2.1#trackList [] ",
            MBE_AlbumGetTrackList =
             "http://musicbrainz.org/mm/mm-2.1#trackList",
            MBE_AlbumGetTrackNum =
             "http://musicbrainz.org/mm/mm-2.1#trackList [?] http://musicbrainz.org/mm/mm-2.1#trackNum",
            MBE_AlbumGetTrackName =
             "http://musicbrainz.org/mm/mm-2.1#trackList [] http://purl.org/dc/elements/1.1/title",
            MBE_AlbumGetTrackDuration =
             "http://musicbrainz.org/mm/mm-2.1#trackList [] http://musicbrainz.org/mm/mm-2.1#duration",
            MBE_AlbumGetArtistName =
             "http://musicbrainz.org/mm/mm-2.1#trackList [] http://purl.org/dc/elements/1.1/creator http://purl.org/dc/elements/1.1/title",
            MBE_AlbumGetArtistSortName =
             "http://musicbrainz.org/mm/mm-2.1#trackList [] http://purl.org/dc/elements/1.1/creator http://musicbrainz.org/mm/mm-2.1#sortName",
            MBE_AlbumGetArtistId =
             "http://musicbrainz.org/mm/mm-2.1#trackList [] http://purl.org/dc/elements/1.1/creator",
            MBE_TrackGetTrackName =
             "http://purl.org/dc/elements/1.1/title",
            MBE_TrackGetTrackId =
             "",
            MBE_TrackGetTrackNum =
             "http://musicbrainz.org/mm/mm-2.1#trackNum",
            MBE_TrackGetTrackDuration =
             "http://musicbrainz.org/mm/mm-2.1#duration",
            MBE_TrackGetArtistName =
             "http://purl.org/dc/elements/1.1/creator http://purl.org/dc/elements/1.1/title",
            MBE_TrackGetArtistSortName =
             "http://purl.org/dc/elements/1.1/creator http://musicbrainz.org/mm/mm-2.1#sortName",
            MBE_TrackGetArtistId =
             "http://purl.org/dc/elements/1.1/creator",
            MBE_QuickGetArtistName =
             "http://musicbrainz.org/mm/mq-1.1#artistName",
            MBE_QuickGetArtistSortName =
             "http://musicbrainz.org/mm/mm-2.1#sortName",
            MBE_QuickGetArtistId =
             "http://musicbrainz.org/mm/mm-2.1#artistid",
            MBE_QuickGetAlbumName =
             "http://musicbrainz.org/mm/mq-1.1#albumName",
            MBE_QuickGetTrackName =
             "http://musicbrainz.org/mm/mq-1.1#trackName",
            MBE_QuickGetTrackNum =
             "http://musicbrainz.org/mm/mm-2.1#trackNum",
            MBE_QuickGetTrackId =
             "http://musicbrainz.org/mm/mm-2.1#trackid",
            MBE_QuickGetTrackDuration =
             "http://musicbrainz.org/mm/mm-2.1#duration",
            MBE_ReleaseGetDate =
             "http://purl.org/dc/elements/1.1/date",
            MBE_ReleaseGetCountry =
             "http://musicbrainz.org/mm/mm-2.1#country",
            MBE_LookupGetType =
             "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
            MBE_LookupGetRelevance =
             "http://musicbrainz.org/mm/mq-1.1#relevance",
            MBE_LookupGetArtistId =
             "http://musicbrainz.org/mm/mq-1.1#artist",
            MBE_LookupGetAlbumId =
             "http://musicbrainz.org/mm/mq-1.1#album",
            MBE_LookupGetAlbumArtistId =
             "http://musicbrainz.org/mm/mq-1.1#album " +
             "http://purl.org/dc/elements/1.1/creator",
            MBE_LookupGetTrackId =
             "http://musicbrainz.org/mm/mq-1.1#track",
            MBE_LookupGetTrackArtistId =
             "http://musicbrainz.org/mm/mq-1.1#track " +
             "http://purl.org/dc/elements/1.1/creator",
            MBE_TOCGetCDIndexId =
             "http://musicbrainz.org/mm/mm-2.1#cdindexid",
            MBE_TOCGetFirstTrack =
             "http://musicbrainz.org/mm/mm-2.1#firstTrack",
            MBE_TOCGetLastTrack =
             "http://musicbrainz.org/mm/mm-2.1#lastTrack",
            MBE_TOCGetTrackSectorOffset =
             "http://musicbrainz.org/mm/mm-2.1#toc [] http://musicbrainz.org/mm/mm-2.1#sectorOffset",
            MBE_TOCGetTrackNumSectors =
             "http://musicbrainz.org/mm/mm-2.1#toc [] http://musicbrainz.org/mm/mm-2.1#numSectors",
            MBE_AuthGetSessionId =
             "http://musicbrainz.org/mm/mq-1.1#sessionId",
            MBE_AuthGetChallenge =
             "http://musicbrainz.org/mm/mq-1.1#authChallenge",
            MBQ_GetCDInfo =
             "@CDINFO@",
            MBQ_GetCDTOC =
             "@LOCALCDINFO@",
            MBQ_AssociateCD =
             "@CDINFOASSOCIATECD@",
            MBQ_Authenticate =
             "<mq:AuthenticateQuery>\n" +
             "   <mq:username>@1@</mq:username>\n" +
             "</mq:AuthenticateQuery>\n",
            MBQ_GetCDInfoFromCDIndexId =
             "<mq:GetCDInfo>\n" +
             "   <mq:depth>@DEPTH@</mq:depth>\n" +
             "   <mm:cdindexid>@1@</mm:cdindexid>\n" +
             "</mq:GetCDInfo>\n",
            MBQ_TrackInfoFromTRMId =
             "<mq:TrackInfoFromTRMId>\n" +
             "   <mm:trmid>@1@</mm:trmid>\n" +
             "   <mq:artistName>@2@</mq:artistName>\n" +
             "   <mq:albumName>@3@</mq:albumName>\n" +
             "   <mq:trackName>@4@</mq:trackName>\n" +
             "   <mm:trackNum>@5@</mm:trackNum>\n" +
             "   <mm:duration>@6@</mm:duration>\n" +
             "</mq:TrackInfoFromTRMId>\n",
            MBQ_QuickTrackInfoFromTrackId =
             "<mq:QuickTrackInfoFromTrackId>\n" +
             "   <mm:trackid>@1@</mm:trackid>\n" +
             "   <mm:albumid>@2@</mm:albumid>\n" +
             "</mq:QuickTrackInfoFromTrackId>\n",
            MBQ_FindArtistByName =
             "<mq:FindArtist>\n" +
             "   <mq:depth>@DEPTH@</mq:depth>\n" +
             "   <mq:artistName>@1@</mq:artistName>\n" +
             "   <mq:maxItems>@MAX_ITEMS@</mq:maxItems>\n" +
             "</mq:FindArtist>\n",
            MBQ_FindAlbumByName =
             "<mq:FindAlbum>\n" +
             "   <mq:depth>@DEPTH@</mq:depth>\n" +
             "   <mq:maxItems>@MAX_ITEMS@</mq:maxItems>\n" +
             "   <mq:albumName>@1@</mq:albumName>\n" +
             "</mq:FindAlbum>\n",
            MBQ_FindTrackByName =
             "<mq:FindTrack>\n" +
             "   <mq:depth>@DEPTH@</mq:depth>\n" +
             "   <mq:maxItems>@MAX_ITEMS@</mq:maxItems>\n" +
             "   <mq:trackName>@1@</mq:trackName>\n" +
             "</mq:FindTrack>\n",
            MBQ_FindDistinctTRMId =
             "<mq:FindDistinctTRMID>\n" +
             "   <mq:depth>@DEPTH@</mq:depth>\n" +
             "   <mq:artistName>@1@</mq:artistName>\n" +
             "   <mq:trackName>@2@</mq:trackName>\n" +
             "</mq:FindDistinctTRMID>\n",
            MBQ_GetArtistById =
             "http://@URL@/mm-2.1/artist/@1@/@DEPTH@",
            MBQ_GetAlbumById =
             "http://@URL@/mm-2.1/album/@1@/@DEPTH@",
            MBQ_GetTrackById =
             "http://@URL@/mm-2.1/track/@1@/@DEPTH@",
            MBQ_GetTrackByTRMId =
             "http://@URL@/mm-2.1/trmid/@1@/@DEPTH@",
            MBQ_SubmitTrackTRMId =
             "<mq:SubmitTRMList>\n" +
             " <mm:trmidList>\n" +
             "  <rdf:Bag>\n" +
             "   <rdf:li>\n" +
             "    <mq:trmTrackPair>\n" +
             "     <mm:trackid>@1@</mm:trackid>\n" +
             "     <mm:trmid>@2@</mm:trmid>\n" +
             "    </mq:trmTrackPair>\n" +
             "   </rdf:li>\n" +
             "  </rdf:Bag>\n" +
             " </mm:trmidList>\n" +
             " <mq:sessionId>@SESSID@</mq:sessionId>\n" +
             " <mq:sessionKey>@SESSKEY@</mq:sessionKey>\n" +
             " <mq:clientVersion>@CLIENTVER@</mq:clientVersion>\n" +
             "</mq:SubmitTRMList>\n",
            MBQ_FileInfoLookup =
             "<mq:FileInfoLookup>\n" +
             "   <mm:trmid>@1@</mm:trmid>\n" +
             "   <mq:artistName>@2@</mq:artistName>\n" +
             "   <mq:albumName>@3@</mq:albumName>\n" +
             "   <mq:trackName>@4@</mq:trackName>\n" +
             "   <mm:trackNum>@5@</mm:trackNum>\n" +
             "   <mm:duration>@6@</mm:duration>\n" +
             "   <mq:fileName>@7@</mq:fileName>\n" +
             "   <mm:artistid>@8@</mm:artistid>\n" +
             "   <mm:albumid>@9@</mm:albumid>\n" +
             "   <mm:trackid>@10@</mm:trackid>\n" +
             "   <mq:maxItems>@MAX_ITEMS@</mq:maxItems>\n" +
             "</mq:FileInfoLookup>\n";
    }
}
