//
// NextArrowButton.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (c) 2008 Scott Peterson
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Gtk;
using Hyena.Gui;
using Hyena.Widgets;

using Banshee.ServiceStack;

namespace Banshee.Gui.Widgets
{
    public class NextArrowButton : ArrowButton
    {
        private InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
        
        public NextArrowButton() : base (ServiceManager.Get<InterfaceActionService> ().PlaybackActions["NextAction"])
        {
        }
        
        protected override IEnumerable<Widget> MenuItems {
            get {
                PlaybackShuffleActions action = service.PlaybackActions.ShuffleActions;
                yield return action["ShuffleOffAction"].CreateMenuItem ();
                yield return new SeparatorMenuItem ();
                yield return action["ShuffleSongAction"].CreateMenuItem ();
                yield return action["ShuffleArtistAction"].CreateMenuItem ();
                yield return action["ShuffleAlbumAction"].CreateMenuItem ();
            }
        }
        
        protected override IRadioActionGroup ActionGroup {
            get { return service.PlaybackActions.ShuffleActions; }
        }
    }
}
