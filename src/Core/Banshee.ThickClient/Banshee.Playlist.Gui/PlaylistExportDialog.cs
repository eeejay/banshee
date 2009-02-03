using System;

using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Widgets;
using Banshee.Playlists.Formats;

namespace Banshee.Playlist.Gui
{
    public class PlaylistExportDialog : Banshee.Gui.Dialogs.FileChooserDialog
    {
        protected ComboBox combobox;
        protected ListStore store;    
        protected PlaylistFormatDescription playlist;
        protected string initial_name;
                
        public PlaylistExportDialog(string name, Window parent) : 
            base(Catalog.GetString("Export Playlist"), parent, FileChooserAction.Save)
        {
            initial_name = FileNamePattern.Escape (name);
            playlist = PlaylistFileUtil.GetDefaultExportFormat();             
            CurrentName = System.IO.Path.ChangeExtension(initial_name, playlist.FileExtension);
            DefaultResponse = ResponseType.Ok;
            DoOverwriteConfirmation = true;            
            
            AddButton(Stock.Cancel, ResponseType.Cancel);
            AddButton(Catalog.GetString("Export"), ResponseType.Ok);
            
            InitializeExtraWidget();                   
        }
        
        protected void InitializeExtraWidget() 
        {               
            PlaylistFormatDescription [] formats = PlaylistFileUtil.ExportFormats;
            int default_export_index = PlaylistFileUtil.GetFormatIndex(formats, playlist);
            
            // Build custom widget used to select the export format.
            store = new ListStore(typeof(string), typeof(PlaylistFormatDescription));
            foreach (PlaylistFormatDescription format in formats) {
                store.AppendValues(format.FormatName, format);
            }
                                    
            HBox hBox = new HBox(false, 2);
            
            combobox = new ComboBox(store);
            CellRendererText crt = new CellRendererText();
            combobox.PackStart(crt, true);
            combobox.SetAttributes(crt, "text", 0);
            combobox.Active = default_export_index;
            combobox.Changed += OnComboBoxChange;
            
            hBox.PackStart(new Label(Catalog.GetString("Select Format: ")), false, false, 0);
            hBox.PackStart(combobox, true, true, 0);
            
            combobox.ShowAll();
            hBox.ShowAll();            
            ExtraWidget = hBox; 
        }
        
        protected void OnComboBoxChange(object o, EventArgs args)
        {
            playlist = GetExportFormat();
            
            if (playlist != null) {
                // Store the export format so that we can default to it the
                // next time the user exports.
                PlaylistFileUtil.SetDefaultExportFormat(playlist);
                
                // If the filename has an extension, update it to the extension
                // of the export format.
                string file_name = null;
                
                if (Filename != null) {
                    file_name = System.IO.Path.GetFileName(Filename);
                }
                                
                if (file_name != null) {
                    CurrentName = System.IO.Path.ChangeExtension(file_name, playlist.FileExtension);
                } else {
                    CurrentName = System.IO.Path.ChangeExtension(initial_name, playlist.FileExtension);
                }
            }            
        }
        
        public PlaylistFormatDescription GetExportFormat() 
        {
            PlaylistFormatDescription selected_playlist = null;
            
            // Get the format that the user selected.
            if (combobox != null && store != null) {
                TreeIter iter;
                if (combobox.GetActiveIter(out iter)) {
                    selected_playlist = store.GetValue(iter, 1) as PlaylistFormatDescription;
                }
            }
            
            return selected_playlist;
        }
    }
}
