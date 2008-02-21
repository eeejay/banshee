//
// Browser.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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

using Hyena;
using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Web
{
    public class Browser
    {
        public static bool Open (string url)
        {
            try {
                System.Diagnostics.Process.Start (
                    System.Uri.EscapeUriString (url)
                );
            } catch(Exception e) {
                Log.Warning (Catalog.GetString ("Could not launch URL"),
                    String.Format (Catalog.GetString ("{0} could not be opened: {1}\n\n " + 
                        "Check your 'Preferred Applications' settings."), url, e.Message), true);
            }
            
            return false;
        }

        public static readonly string UserAgent = String.Format ("Banshee {0} (http://banshee-project.org/", 
            Application.Version);
    }
}
