
using System;
using Gtk;
using Banshee.Sources;
using Banshee.Sources.Gui;

namespace Banshee.Lastfm.Radio
{
    public class LastfmSourceContents : Gtk.Label, ISourceContents
    {
        private Source source;
        public LastfmSourceContents () : base ("Coming Soon: Profile, Friends, Recently Loved Songs, etc")
        {
        }

        public bool SetSource (Source source)
        {
            this.source = source;
            return true;
        }

        public Source Source {
            get { return source; }
        }

        public void ResetSource ()
        {
            source = null;
        }

        public Widget Widget {
            get { return this; }
        }
    }
}
