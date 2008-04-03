//
// AudioCdRipper.cs
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
using Mono.Addins;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.MediaEngine;

namespace Banshee.AudioCd
{
    public class AudioCdRipper
    {
        private static bool ripper_extension_queried = false;
        private static TypeExtensionNode ripper_extension_node = null;
        
        public static bool Supported {
            get { 
                if (ripper_extension_queried) {
                    return ripper_extension_node != null;
                }
                
                ripper_extension_queried = true;
                
                foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (
                    "/Banshee/MediaEngine/AudioCdRipper")) {
                    ripper_extension_node = node;
                    break;
                }
                
                return ripper_extension_node != null;
            }
        }
        
        private IAudioCdRipper ripper;
        private AudioCdSource source;
        private UserJob user_job;
         
        public AudioCdRipper (AudioCdSource source)
        {
            if (ripper_extension_node != null) {
                ripper = (IAudioCdRipper)ripper_extension_node.CreateInstance ();
            } else {
                throw new ApplicationException ("No AudioCdRipper extension is installed");
            }
            
            this.source = source;
        }
        
        public void Start ()
        {
            source.LockAllTracks ();
            ripper.Begin ();
            
            user_job = new UserJob (Catalog.GetString ("Importing Audio CD"), Catalog.GetString ("Initializing Drive"));
            user_job.IconNames = new string [] { "cd-action-rip" };
            user_job.CanCancel = true;
            user_job.Register ();
        }
    }
}
