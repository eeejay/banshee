
/***************************************************************************
 *  ToggleState.cs
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
using Gtk;

namespace Banshee.Widgets 
{
    abstract public class ToggleState
    {
        private Gdk.Pixbuf icon;
        private string label;
        private ToggleAction toggle_action;
        private bool check_active;
        private bool match_active;
        private bool match_value;
        
        protected ToggleState()
        {
        }
        
        internal ToggleAction ToggleAction {
            get {
                return toggle_action;
            }
            
            set {
                toggle_action = value;
            }
        }
        
        internal bool CheckActive {
            get {
                return check_active;
            }
            
            set {
                check_active = value;
            }
        }
      
        public Gdk.Pixbuf Icon {
            get {
                return icon;
            }
            
            protected set {
                icon = value;
            }
        }
        
        public string Label {
            get {
                return label;
            }
            
            protected set {
                label = value;
            }
        }

        public bool MatchActive {
            get {
                return match_active;
            }

            set {
                match_active = value;
            }
        }

        public bool MatchValue {
            get {
                return match_value;
            }

            set {
                match_value = value;
            }
        }
    }
}
