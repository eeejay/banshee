//
// DirectoryScannerPipelineElement.cs
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

using Hyena.Collections;

using Banshee.Base;

namespace Banshee.IO
{  
    public class DirectoryScannerPipelineElement : QueuePipelineElement<string>
    {
        protected override string ProcessItem (string item)
        {
            ScanForFiles (item);
            return null;
        }
        
        private void ScanForFiles (string source)
        {
            CheckForCanceled ();
            
            bool is_regular_file = false;
            bool is_directory = false;
            
            SafeUri source_uri = new SafeUri (source);
            
            try {
                is_regular_file = Banshee.IO.File.Exists (source_uri);
                is_directory = !is_regular_file && Banshee.IO.Directory.Exists (source);
            } catch {
                return;
            }
            
            if (is_regular_file) {
                try {
                    if (!Path.GetFileName (source).StartsWith (".")) {
                        EnqueueDownstream (source);
                    }
                } catch (System.ArgumentException) {
                    // If there are illegal characters in path
                }
            } else if (is_directory) {
                try {
                    if (!Path.GetFileName (Path.GetDirectoryName (source)).StartsWith (".")) {
                        try {
                            foreach (string file in Banshee.IO.Directory.GetFiles (source)) {
                                ScanForFiles (file);
                            }

                            foreach (string directory in Banshee.IO.Directory.GetDirectories (source)) {
                                ScanForFiles (directory);
                            }
                        } catch {
                        }
                    }
                } catch (System.ArgumentException) {
                    // If there are illegal characters in path
                }
            }
        }
    }
}
