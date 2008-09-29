//
// XmlColumnController.cs
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
using System.IO;
using System.Text;
using System.Xml;
using System.Reflection;

using Hyena.Data;
using Hyena.Data.Gui;

namespace Banshee.Collection.Gui
{
    public class XmlColumnController : DefaultColumnController
    {
        public XmlColumnController (string xml) : base (false)
        {
            XmlTextReader reader = new XmlTextReader (new StringReader (xml));
            
            while (reader.Read ()) {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "column-controller") {
                    ReadColumnController (reader, reader.Depth);
                }
            }
            
            Load ();
        }
        
        private void ReadColumnController (XmlTextReader reader, int depth)
        {
            string sort_column = null;
            bool sort_asc = true;
            
            while (reader.Read ()) {
                if (reader.NodeType == XmlNodeType.Element) {
                    switch (reader.Name) {
                        case "sort-column":
                            sort_asc = (reader.GetAttribute ("direction") != "desc");
                            sort_column = reader.ReadString ();
                            break;
                        case "column": ReadColumn (reader, reader.Depth); break;
                        case "add-all-defaults": AddDefaultColumns (); break;
                        case "add-default":
                        case "remove-default":
                            bool add_col = reader.Name[0] == 'a';
                            while (reader.MoveToNextAttribute ()) {
                                if (reader.Name == "column") {
                                    Column col = GetDefaultColumn (reader.Value);
                                    if (col != null) {
                                        if (add_col) {
                                            Add (col);
                                        } else {
                                            Remove (col);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                } else if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth) {
                    break;
                }
            }
            
            if (sort_column != null) {
                foreach (Column col in Columns) {
                    if (col.Id == sort_column) {
                        if (col is SortableColumn) {
                            DefaultSortColumn = col as SortableColumn;
                            DefaultSortColumn.SortType = sort_asc ? SortType.Ascending : SortType.Descending;
                        } else {
                            Hyena.Log.WarningFormat ("Defined default sort column {0} is not a SortableColumn", sort_column);
                        }
                        break;
                    }
                }
            }
        }
        
        private void ReadColumn (XmlTextReader reader, int depth)
        {
            string modify_default = null;
            
            string title = null;
            string long_title = null;
            string sort_key = null;
            double width = -1;
            int max_width = -1;
            int min_width = -1;
            bool visible = true;
            
            string renderer_type = null;
            string renderer_property = null;
            bool renderer_expand = true;
            
            while (reader.MoveToNextAttribute ()) {
                if (reader.Name == "modify-default") {
                    modify_default = reader.Value;
                    break;
                }
            }
                
            while (reader.Read ()) {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth) {
                    break;
                } else if (reader.NodeType != XmlNodeType.Element) {
                    continue;
                }
                
                switch (reader.Name) {
                    case "title": title = reader.ReadString (); break;
                    case "long-title": long_title = reader.ReadString (); break;
                    case "sort-key": sort_key = reader.ReadString (); break;
                    case "width": width = reader.ReadElementContentAsDouble (); break;
                    case "max-width": max_width = reader.ReadElementContentAsInt (); break;
                    case "min-width": min_width = reader.ReadElementContentAsInt (); break;
                    case "visible": visible = ParseBoolean (reader.ReadString ()); break;
                    case "renderer":
                        while (reader.MoveToNextAttribute ()) {
                            switch (reader.Name) {
                                case "type": renderer_type = reader.Value; break;
                                case "property": renderer_property = reader.Value; break;
                                case "expand": renderer_expand = ParseBoolean (reader.Value); break;
                            }
                        }
                        break;
                }
            }
            
            if (modify_default != null) {
                Column column = GetDefaultColumn (modify_default);
                    
                if (title != null) {
                    column.Title = title;
                }

                if (long_title != null) {
                    column.LongTitle = long_title;
                }
                
                if (renderer_type != null) {
                    ColumnCell renderer = GetCellRenderer (renderer_type, renderer_property, renderer_expand);
                    column.RemoveCell (0);
                    column.PackStart (renderer);
                }
                
                if (renderer_property != null) {
                    column.GetCell (0).Property = renderer_property;
                }
                
                if (column.Visible != visible) {
                    column.Visible = visible;
                }
                
                if (column is SortableColumn && sort_key != null) {
                    ((SortableColumn)column).SortKey = sort_key;
                }
            } else {
                ColumnCell renderer = GetCellRenderer (renderer_type, renderer_property, renderer_expand);
                
                Column column = sort_key == null
                    ? new Column (title, renderer, width, visible)
                    : new SortableColumn (title, renderer, width, sort_key, visible);

                if (max_width != -1) {
                    column.MaxWidth = max_width;
                }

                if (min_width != -1) {
                    column.MinWidth = min_width;
                }
                
                Add (column);
            }
        }
        
        private ColumnCell GetCellRenderer (string typeName, string property, bool expand)
        {
            Type type = null;
                
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ()) {
                type = asm.GetType (typeName, false, true);
                if (type != null) {
                    break;
                }
            }
            
            if (type == null) {
                throw new TypeLoadException (typeName);
            }
            
            return (ColumnCell)Activator.CreateInstance (type, property, expand);
        }
        
        private bool ParseBoolean (string value)
        {
            value = value.ToLower ();
            return value == "true";
        }
        
        private Column GetDefaultColumn (string propertyName)
        {
            PropertyInfo property = GetType ().GetProperty (propertyName);
            return property == null ? null : property.GetValue (this, null) as Column;
        }
    }
}
