/***************************************************************************
 *  PlayerWindowSchema.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
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
using Banshee.Configuration;
 
namespace Banshee.Configuration.Schema
{
    public static class PlayerWindowSchema
    {
        public static readonly SchemaEntry<int> Width = new SchemaEntry<int>(
            "player_window", "width",
            1024,
            "Window Width",
            "Width of the main interface window."
        );

        public static readonly SchemaEntry<int> Height = new SchemaEntry<int>(
            "player_window", "height",
            700,
            "Window Height",
            "Height of the main interface window."
        );

        public static readonly SchemaEntry<int> XPos = new SchemaEntry<int>(
            "player_window", "x_pos",
            0,
            "Window Position X",
            "Pixel position of Main Player Window on the X Axis"
        );

        public static readonly SchemaEntry<int> YPos = new SchemaEntry<int>(
            "player_window", "y_pos",
            0,
            "Window Position Y",
            "Pixel position of Main Player Window on the Y Axis"
        );

        public static readonly SchemaEntry<bool> Maximized = new SchemaEntry<bool>(
            "player_window", "maximized",
            false,
            "Window Maximized",
            "True if main window is to be maximized, false if it is not."
        );

        public static readonly SchemaEntry<int> SourceViewWidth = new SchemaEntry<int>(
            "player_window", "source_view_width",
            175,
            "Source View Width",
            "Width of Source View Column."
        );

        public static readonly SchemaEntry<bool> ShowCoverArt = new SchemaEntry<bool>(
            "player_window", "show_cover_art",
            true,
            "Show cover art",
            "Show cover art below source view if available"
        );
        
        public static readonly SchemaEntry<bool> PlaybackShuffle = new SchemaEntry<bool>(
            "playback", "shuffle",
            false,
            "Shuffle playback",
            "Enable shuffle mode"
        );
        
        public static readonly SchemaEntry<int> PlaybackRepeat = new SchemaEntry<int>(
            "playback", "repeat",
            0,
            "Repeat playback",
            "Repeat mode (0 = None, 1 = All, 2 = Single)"
        );
    }
}
