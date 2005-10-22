/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  SimpleTrack.cs
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
    public class SimpleTrack
    {
        private string artist;
        private string title;
        private int index;
        private int length;
        
        public SimpleTrack(int index, int length)
        {
            this.index = index;
            this.length = length;
        }
        
        public string Artist {
            get {
                return artist;
            }
            
            set {
                artist = value;
            }
        }
        
        public string Title {
            get {
                return title;
            }
            
            set {
                title = value;
            }
        }
        
        public int Index {
            get {
                return index;
            }
            
            set {
                index = value;
            }
        }
        
        public int Length {
            get {
                return length;
            }
            
            set {
                length = value;
            }
        }
        
        public int Minutes {
            get {
                return length / 60;
            }
        }
        
        public int Seconds {
            get {
                return length % 60;
            }
        }
    }
}
