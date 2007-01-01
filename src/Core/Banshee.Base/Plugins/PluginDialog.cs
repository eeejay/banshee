/***************************************************************************
 *  PluginDialog.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
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
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Configuration;

namespace Banshee.Plugins
{
    internal class PluginDialog : Dialog
    {
        private TreeView plugin_tree;
        private ListStore plugin_store;
        private Label name_label;
        private Label description_label;
        private Label authors_label;
        private HBox disabled_box;
        private Notebook plugin_notebook;
        
        private Gdk.Color normal_color;
        private Gdk.Color disabled_color;
    
        public PluginDialog() : base(
            Catalog.GetString("Banshee Plugins"),
            null,
            DialogFlags.Modal | DialogFlags.NoSeparator,
            Stock.Close,
            ResponseType.Close)
        {
            BorderWidth = 10;
            HBox box = new HBox();
            box.Spacing = 10;
            
            plugin_tree = new TreeView();
            
            TreeViewColumn name_column = new TreeViewColumn(Catalog.GetString("Plugin Name"), 
                new CellRendererText(), "text", 0, "foreground-gdk", 3);
            name_column.Expand = true;
            plugin_tree.AppendColumn(name_column);
            
            CellRendererToggle toggle_cell = new CellRendererToggle();
            TreeViewColumn toggle_column = new TreeViewColumn(Catalog.GetString("Enabled"), 
                toggle_cell, "active", 1);
            toggle_cell.Activatable = true;
            toggle_cell.Toggled += OnActiveToggled;
            toggle_column.Expand = false;
            plugin_tree.AppendColumn(toggle_column);
            
            plugin_tree.CursorChanged += OnCursorChanged;
            
            ScrolledWindow scroll = new ScrolledWindow();
            scroll.Add(plugin_tree);
            scroll.ShadowType = ShadowType.In;
            scroll.HscrollbarPolicy = PolicyType.Never;
            
            plugin_notebook = new Notebook();
            
            VBox info_box = new VBox();
            info_box.BorderWidth = 12;
            info_box.Spacing = 10;
            
            plugin_notebook.AppendPage(info_box, new Label(Catalog.GetString("Overview")));
            
            name_label = new Label();
            CreateInfoRow(info_box, Catalog.GetString("Plugin Name"), name_label);
            
            description_label = new Label();
            description_label.LineWrap = true;
            CreateInfoRow(info_box, Catalog.GetString("Description"), description_label);
            
            authors_label = new Label();
            CreateInfoRow(info_box, Catalog.GetString("Author(s)"), authors_label);
            
            disabled_box = new HBox();
            disabled_box.Spacing = 5;
            Image disabled_image = new Image(Stock.DialogError, IconSize.Menu);
            Label disabled_label = new Label(Catalog.GetString("This plugin could not be initialized."));
            disabled_label.Xalign = 0.0f;
            disabled_box.PackStart(disabled_image, false, false, 0);
            disabled_box.PackStart(disabled_label, true, true, 0);
            info_box.PackStart(disabled_box, false, false, 0);
            
            box.PackStart(scroll, false, false, 0);
            box.PackStart(plugin_notebook, true, true, 0);
            VBox.PackStart(box, true, true, 0);
            
            VBox.Spacing = 10;
            
            normal_color = plugin_tree.Style.Foreground(StateType.Normal);
            disabled_color = plugin_tree.Style.Foreground(StateType.Insensitive);
            
            FillTree();
            plugin_tree.ColumnsAutosize();
            
            IconThemeUtils.SetWindowIcon(this);
            box.ShowAll();
            disabled_box.Hide();
            
            WindowPosition = WindowPosition.Center;
        }
        
        private void CreateInfoRow(Box parent, string name, Label valueLabel)
        {
            VBox box = new VBox();
            box.Spacing = 2;
            Label label = new Label();
            label.Xalign = 0.0f;
            label.Markup = "<b>" + GLib.Markup.EscapeText(name) + "</b>";
            valueLabel.Xalign = 0.0f;
            box.PackStart(label, false, false, 0);
            box.PackStart(valueLabel, false, false, 0);
            parent.PackStart(box, false, false, 0);
        }
        
        private void FillTree()
        {
            plugin_store = new ListStore(typeof(string), typeof(bool), typeof(Plugin), typeof(Gdk.Color));
            
            foreach(Plugin plugin in PluginCore.Factory) {
                plugin_store.AppendValues(plugin.DisplayName, plugin.Initialized, plugin, 
                    plugin.Broken ? disabled_color : normal_color);
            }
            
            plugin_tree.Model = plugin_store;
        }
        
        private void OnCursorChanged(object o, EventArgs args)
        {
            TreeIter iter;
            
            if(!plugin_tree.Selection.GetSelected(out iter)) {
                return;
            }
            
            Plugin plugin = plugin_store.GetValue(iter, 2) as Plugin;
            
            if(plugin == null) {
                return;
            }
            
            string authors = String.Empty;
            foreach(string author in plugin.Authors) {
                authors += author + "\n";
            }
            
            name_label.Text = plugin.DisplayName;
            description_label.Text = plugin.Description;
            authors_label.Text = authors;
            
            if(plugin.Broken) {
                disabled_box.ShowAll();
            } else {
                disabled_box.Hide();
            }
            
            if(plugin_notebook.NPages > 1) {
                plugin_notebook.RemovePage(1);
            }
            
            if(!plugin.Broken && plugin.Initialized && plugin.HasConfigurationWidget) {
                Alignment alignment = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
                alignment.BorderWidth = 12;
                Widget widget = plugin.GetConfigurationWidget();
                alignment.Add(widget);
                plugin_notebook.AppendPage(alignment, new Label(Catalog.GetString("Configuration")));
                widget.Show();
                alignment.Show();
            } 
        }

        private void OnActiveToggled(object o, ToggledArgs args) 
        {
            TreeIter iter;

            if(plugin_store.GetIter(out iter, new TreePath(args.Path))) {
                Plugin plugin = plugin_store.GetValue(iter, 2) as Plugin;
                bool initialize = !(bool)plugin_store.GetValue(iter, 1);
                
                if(plugin == null || plugin.Broken) {
                    return;
                }
                
                if(initialize) {
                    plugin.Initialize();
                } else {
                    plugin.Dispose();
                }
                
                plugin_store.SetValue(iter, 1, plugin.Initialized);
                plugin_store.SetValue(iter, 3, plugin.Broken ? disabled_color : normal_color);
                
                ConfigurationClient.Set<bool>(plugin.ConfigurationNamespace, "enabled", plugin.Initialized);
            }
        }
    }
}
