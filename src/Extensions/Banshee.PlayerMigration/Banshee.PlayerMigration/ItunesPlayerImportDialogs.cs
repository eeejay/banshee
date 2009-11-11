//
// ItunesPlayerImportDialogs.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2007 Scott Peterson
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
using System.IO;
using Gtk;
using Mono.Unix;

using Banshee.Base;

namespace Banshee.PlayerMigration
{
    public class ItunesImportDialog : Dialog
    {
        private string library_uri;
        protected readonly Button import_button;
        protected readonly CheckButton ratings;
        protected readonly CheckButton stats;
        protected readonly CheckButton playlists;

        public string LibraryUri
        {
            get { return library_uri; }
        }

        public bool Ratings
        {
            get { return ratings.Active; }
        }
        public bool Stats
        {
            get { return stats.Active; }
        }
        public bool Playliststs
        {
            get { return playlists.Active; }
        }

        public ItunesImportDialog () : base ()
        {
            // TODO add Help button and dialog/tooltip

            Title = Catalog.GetString ("iTunes Importer");
            Resizable = false;
            VBox.BorderWidth = 8;
            VBox.Spacing = 8;

            Button cancel_button = new Button (Stock.Cancel);
            cancel_button.Clicked += delegate { Respond (ResponseType.Cancel); };
            cancel_button.ShowAll ();
            AddActionWidget (cancel_button, ResponseType.Cancel);
            cancel_button.CanDefault = true;
            cancel_button.GrabFocus ();
            DefaultResponse = ResponseType.Cancel;

            import_button = new Button ();
            import_button.Label = Catalog.GetString ("_Import");
            import_button.UseUnderline = true;
            import_button.Image = Image.NewFromIconName (Stock.Open, IconSize.Button);
            import_button.Clicked += delegate { Respond (ResponseType.Ok); };
            import_button.ShowAll ();
            AddActionWidget (import_button, ResponseType.Ok);

            VBox vbox = new VBox ();
            ratings = new CheckButton (Catalog.GetString ("Import song ratings"));
            ratings.Active = true;
            vbox.PackStart (ratings);
            stats = new CheckButton (Catalog.GetString ("Import play statistics (playcount, etc.)"));
            stats.Active = true;
            vbox.PackStart (stats);
            playlists = new CheckButton (Catalog.GetString ("Import playlists"));
            playlists.Active = true;
            vbox.PackStart (playlists);

            PackCheckboxes (vbox);

            VBox.ShowAll ();
        }

        protected virtual void PackCheckboxes (VBox vbox)
        {
            string possible_location = System.IO.Path.Combine (System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "iTunes"),
                ItunesPlayerImportSource.LibraryFilename);

            if (Banshee.IO.File.Exists (new SafeUri (possible_location))) {
                library_uri = possible_location;
            } else {
                HBox hbox = new HBox ();
                hbox.Spacing = 8;
                Image image = new Image (IconTheme.Default.LoadIcon ("gtk-open", 18, 0));
                hbox.PackStart (image);
                Label label1 = new Label ();
                label1.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText(
                    String.Format( Catalog.GetString (@"Locate your ""{0}"" file..."),
                    ItunesPlayerImportSource.LibraryFilename)));
                label1.SetAlignment (0.0f, 0.5f);
                hbox.PackStart (label1);
                Button browse_button = new Button (hbox);
                browse_button.Clicked += OnBrowseButtonClicked;
                VBox.PackStart (browse_button);

                ratings.Sensitive = stats.Sensitive = playlists.Sensitive = import_button.Sensitive = false;
            }

            VBox.PackStart (vbox);
        }

        private void OnBrowseButtonClicked (object o, EventArgs args)
        {
            Button browse_button = o as Button;
            using(FileChooserDialog file_chooser = new FileChooserDialog(
                String.Format (Catalog.GetString (@"Locate ""{0}"""), ItunesPlayerImportSource.LibraryFilename),
                this, FileChooserAction.Open,
                Stock.Cancel, ResponseType.Cancel,
                Stock.Open, ResponseType.Ok)) {

                FileFilter filter = new FileFilter ();
                filter.AddPattern ("*" + ItunesPlayerImportSource.LibraryFilename);
                filter.Name = ItunesPlayerImportSource.LibraryFilename;
                file_chooser.AddFilter (filter);
                if (file_chooser.Run () == (int)ResponseType.Ok) {
                    browse_button.Sensitive = false;
                    ratings.Sensitive = stats.Sensitive = playlists.Sensitive = import_button.Sensitive = true;
                    library_uri = file_chooser.Filename;
                }
                file_chooser.Destroy ();
            }
        }
    }

    public class ItunesMusicDirectoryDialog : Dialog
    {
        private FileChooserWidget chooser;

        public string UserMusicDirectory {
            get { return chooser.Filename; }
        }

        public ItunesMusicDirectoryDialog (string itunes_music_directory) : base ()
        {
            Title = Catalog.GetString ("Locate iTunes Music Directory");
            HeightRequest = 650;
            WidthRequest = 814;

            Button cancel_button = new Button (Stock.Cancel);
            cancel_button.Clicked += delegate { Respond (ResponseType.Cancel); };
            cancel_button.ShowAll ();
            AddActionWidget (cancel_button, ResponseType.Cancel);
            cancel_button.CanDefault = true;
            cancel_button.GrabFocus ();

            Button ok_button = new Button (Stock.Ok);
            ok_button.Clicked += delegate { Respond (ResponseType.Ok); };
            ok_button.ShowAll ();
            AddActionWidget (ok_button, ResponseType.Ok);

            VBox vbox = new VBox ();
            vbox.BorderWidth = 8;
            vbox.Spacing = 10;

            HBox hbox = new HBox ();
            hbox.Spacing = 10;

            Image image = new Image (Stock.DialogWarning, IconSize.Dialog);
            hbox.PackStart (image, false, true, 0);

            Label message = new Label ();
            message.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText(
                String.Format (Catalog.GetString(
                    "The iTunes library refers to your music directory as \"{0}\" but " +
                    "Banshee was not able to infer the location of this directory. Please locate it."),
                itunes_music_directory)));
            message.Justify = Justification.Left;
            message.WidthRequest = 750;
            message.LineWrap = true;
            hbox.PackStart (message, true, true, 0);

            vbox.PackStart (hbox, false, true, 0);

            chooser = new FileChooserWidget (FileChooserAction.SelectFolder);
            chooser.ShowAll ();
            vbox.PackStart (chooser, true, true, 0);

            VBox.PackStart (vbox);

            DefaultResponse = ResponseType.Cancel;

            VBox.ShowAll ();
        }
    }
}