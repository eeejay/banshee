//
// MuinsheeActions.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Mono.Unix;
using Gtk;
using Hyena;
using Banshee.Gui;

namespace Muinshee
{
    public class MuinsheeActions : BansheeActionGroup
    {       
        public MuinsheeActions () : base ("muinshee")
        {
            AddImportant (
                new ActionEntry (
                    "PlaySongAction", Stock.Add,
                     Catalog.GetString ("_Play Song"),
                     null, Catalog.GetString ("Add a song to the playlist"), OnPlaySong
                ),
                new ActionEntry (
                    "PlayAlbumAction", Stock.Add,
                     Catalog.GetString ("_Play Album"),
                     null, Catalog.GetString ("Add an album to the playlist"), OnPlayAlbum
                )
            );
            
            AddUiFromFile ("GlobalUI.xml");
        }

        private void OnPlaySong (object sender, EventArgs args)
        {
        }

        private void OnPlayAlbum (object sender, EventArgs args)
        {
        }
    }
}
