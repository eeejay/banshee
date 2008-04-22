//
// Page.cs
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

using Banshee.Library;
using Banshee.Configuration.Schema;

namespace Banshee.Preferences
{
    public class Page : Collection<Section>
    {
        public Page ()
        {
        }
        
        public Page (string id, string name, int order)
        {
            Id = id;
            Name = name;
            Order = order;
        }
        
        internal static void SetupDefaults (PreferenceService service)
        {
            Page general = service.Add (new Page ("general", Catalog.GetString ("General"), 0));
            Section music_library = general.Add (new Section ("music-library", Catalog.GetString ("Music _Library"), 0));
            music_library.Add (new LibraryLocationPreference ());
            music_library.Add (new SchemaPreference<bool> (LibrarySchema.CopyOnImport, Catalog.GetString ("Co_py files to media folders when importing")));
            music_library.Add (new SchemaPreference<bool> (LibrarySchema.WriteMetadata, Catalog.GetString ("Write _metadata to files")));
        }
    }
}
