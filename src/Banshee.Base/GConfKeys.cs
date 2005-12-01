/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  GConfKeys.cs
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
 
namespace Banshee.Base
{
    public sealed class GConfKeys
    {
        public const string BasePath = "/apps/Banshee/";
        
        public const string LibraryLocation = BasePath + "LibraryLocation";
        public const string WindowX = BasePath + "WindowX";
        public const string WindowY = BasePath + "WindowY";
        public const string WindowWidth = BasePath + "WindowWidth";
        public const string WindowHeight = BasePath + "WindowHeight";
        public const string WindowMaximized = BasePath + "WindowMaximized";
        public const string SourceViewWidth = BasePath + "SourceViewWidth";
        public const string SearchBarVisible = BasePath + "SearchBarVisible";
        public const string Volume = BasePath + "Volume";
        public const string PlaylistShuffle = BasePath + "PlaylistShuffle";
        public const string PlaylistRepeat = BasePath + "PlaylistRepeat";
        public const string CopyOnImport = BasePath + "CopyOnImport";
        public const string MoveOnInfoSave = BasePath + "MoveOnInfoSave";
        public const string AdditionAction = BasePath + "AdditionAction";
        public const string PlayerEngine = BasePath + "PlayerEngine";
        public const string FileNamePattern = BasePath + "FileNamePattern";
        public const string EncoderProfiles = BasePath + "EncoderProfiles";
        public const string RippingProfile = BasePath + "RippingProfile";
        public const string RippingBitrate = BasePath + "RippingBitrate";
        public const string IpodProfile = BasePath + "IpodProfile";
        public const string IpodBitrate = BasePath + "IpodBitrate";
        public const string LastFileSelectorUri = BasePath + "LastFileSelectorUri";
        public const string ColumnPath = BasePath + "PlaylistColumns/";
        public const string ShowNotificationAreaIcon = BasePath + "ShowNotificationAreaIcon";
        public const string EnableFileSystemMonitoring = BasePath + "EnableFileSystemMonitoring";
        public const string CDBurnerRoot = BasePath + "CDBurnerOptions/";
        public const string CDBurnerId = BasePath + "CDBurnerId";
        public const string TrackPropertiesExpanded = BasePath + "TrackPropertiesExpanded";
        public const string EnableSpecialKeys = BasePath + "EnableSpecialKeys";
    }
}
