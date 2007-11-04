//
// Layout.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Text;
using System.Collections.Generic;

namespace Hyena.CommandLine
{
    public class Layout
    {
        private List<LayoutGroup> groups;
        
        public Layout (List<LayoutGroup> groups)
        {
            this.groups = groups;
        }

        public Layout (params LayoutGroup [] groups) : this (new List<LayoutGroup> (groups))
        {
        }
        
        public override string ToString ()
        {
            StringBuilder builder = new StringBuilder ();
            
            int terminal_width = Console.WindowWidth <= 0 ? 80 : Console.WindowWidth;
            int min_spacing = 6;
            
            int group_index = 0;
            int max_option_length = 0;
            int max_description_length = 0;
            int description_alignment = 0;
            
            foreach (LayoutGroup group in groups) {
                foreach (LayoutOption option in group) {
                    if (option.Name.Length > max_option_length) {
                        max_option_length = option.Name.Length;
                    }
                }
            }
            
            max_description_length = terminal_width - max_option_length - min_spacing - 4;
            description_alignment = max_option_length + min_spacing + 4;
            
            foreach (LayoutGroup group in groups) {
                if (group.Id != "default") {
                    builder.Append (group.Title);
                    builder.Append ("\n\n");
                }
                
                foreach (LayoutOption option in group) {            
                    int spacing = (max_option_length - option.Name.Length) + min_spacing;
                    builder.AppendFormat ("  --{0}{2}{1}\n", option.Name, 
                        WrapAlign (option.Description, max_description_length, description_alignment),
                        String.Empty.PadRight (spacing));
                }
                
                if (group_index++ < groups.Count - 1) {
                    builder.Append ("\n");
                }
            }
            
            return builder.ToString ();
        }
        
        private static string WrapAlign (string str, int width, int align)
        {
            StringBuilder builder = new StringBuilder ();
            bool did_wrap = false;
            
            for (int i = 0, b = 0; i < str.Length; i++, b++) {
                if (str[i] == ' ') {
                    int word_length = 0;
                    for (int j = i + 1; j < str.Length && str[j] != ' '; word_length++, j++);
                    
                    if (b + word_length >= width) {
                        builder.AppendFormat ("\n{0}", String.Empty.PadRight (align));
                        b = 0;
                        did_wrap = true;
                        continue;
                    }
                }
                
                builder.Append (str[i]);
            }
            
            if (did_wrap) {
                builder.Append ('\n');
            }
            
            return builder.ToString ();
        }
        
        public void Add (LayoutGroup group)
        {
            groups.Add (group);
        }
        
        public void Remove (LayoutGroup group)
        {
            groups.Remove (group);
        }
        
        public void Remove (string groupId)
        {
            LayoutGroup group = FindGroup (groupId);
            if (group != null) {
                groups.Remove (group);
            }
        }
        
        private LayoutGroup FindGroup (string id)
        {
            foreach (LayoutGroup group in groups) {
                if (group.Id == id) {
                    return group;
                }
            }
            
            return null;
        }
        
        public static LayoutOption Option (string name, string description)
        {
            return new LayoutOption (name, description);
        }
        
        public static LayoutGroup Group (string id, string title, params LayoutOption [] options)
        {
            return new LayoutGroup (id, title, options);
        }
    }
}
