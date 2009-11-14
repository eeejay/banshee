//
// InternetRadioSourceContents.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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

using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.Base;
using Banshee.Configuration;

using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.ServiceStack;

using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Collection.Gui;

namespace Banshee.InternetRadio
{
    public class InternetRadioSourceContents : FilteredListSourceContents, ITrackModelSourceContents
    {
        private TrackListView track_view;
        private QueryFilterView<string> genre_view;

        public InternetRadioSourceContents () : base ("iradio")
        {
        }

        protected override void InitializeViews ()
        {
            SetupMainView (track_view = new TrackListView ());
            SetupFilterView (genre_view = new QueryFilterView<string> (Catalog.GetString ("Not Set")));
        }

        protected override void ClearFilterSelections ()
        {
            if (genre_view.Model != null) {
                genre_view.Selection.Clear ();
            }
        }

        protected override bool ActiveSourceCanHasBrowser {
            get { return true; }
        }

        protected override string ForcePosition {
            get { return "left"; }
        }

        #region Implement ISourceContents

        public override bool SetSource (ISource source)
        {
            DatabaseSource track_source = source as DatabaseSource;
            if (track_source == null) {
                return false;
            }

            base.source = source;

            SetModel (track_view, track_source.TrackModel);

            foreach (IListModel model in track_source.CurrentFilters) {
                IListModel<QueryFilterInfo<string>> genre_model = model as IListModel<QueryFilterInfo<string>>;
                if (genre_model != null) {
                    SetModel (genre_view, genre_model);
                }
            }

            return true;
        }

        public override void ResetSource ()
        {
            base.source = null;
            track_view.SetModel (null);
            genre_view.SetModel (null);
        }

        #endregion

        #region ITrackModelSourceContents implementation

        public IListView<TrackInfo> TrackView {
            get { return track_view; }
        }

        #endregion
    }
}
