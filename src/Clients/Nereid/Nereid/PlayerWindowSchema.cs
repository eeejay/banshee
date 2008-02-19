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
 
namespace Nereid
{
    public static class PlayerWindowSchema
    {
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
