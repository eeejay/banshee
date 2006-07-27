/***************************************************************************
 *  DragDropTarget.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using Gtk;

namespace Banshee.Gui.DragDrop
{
    public enum DragDropTargetType {
        Source,
        PlaylistRows,
        TrackInfoObjects,
        UriList
    };
        
    public static class DragDropTarget
    {
        public static readonly TargetEntry Source = 
            new TargetEntry("application/x-banshee-source", TargetFlags.App, 
                (uint)DragDropTargetType.Source);

        public static readonly TargetEntry PlaylistRows = 
            new TargetEntry("application/x-banshee-playlist-rows", TargetFlags.App, 
                (uint)DragDropTargetType.PlaylistRows);

        public static readonly TargetEntry TrackInfoObjects = 
            new TargetEntry("application/x-banshee-track-info-objects", TargetFlags.App, 
                (uint)DragDropTargetType.TrackInfoObjects);

        public static readonly TargetEntry UriList = 
            new TargetEntry("text/uri-list", 0, (uint)DragDropTargetType.UriList);
    }
}
