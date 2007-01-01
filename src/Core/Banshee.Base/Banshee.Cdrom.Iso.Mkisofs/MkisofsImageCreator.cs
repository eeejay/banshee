/***************************************************************************
 *  MkisofsImageCreator.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.IO;
using System.Diagnostics;

using Banshee.Cdrom.Iso;

namespace Banshee.Cdrom.Iso.Mkisofs
{
    public class MkisofsImageCreator : IImageCreator
    {
        private string graft_paths = String.Empty; 
        private bool use_utf8 = false;
        private long image_size = 0;
    
        public event ImageStatusHandler ImageStatusChanged;
    
        private static string EscapeLayoutPath(string layoutPath)
        {
            string path = layoutPath.Replace("\\", "\\\\").Replace("=", "\\=");
            if(!path.StartsWith("/")) {
                path = "/" + path;
            }
            return path;
        }
    
        public void AddPath(string layoutPath, string path)
        {
            graft_paths += String.Format("\"{0}={1}\" ", EscapeLayoutPath(layoutPath), path);
        }
        
        private static bool TestCharset(string charset)
        {
            MkisofsProcess process = new MkisofsProcess();
            process.StartInfo.Arguments = "-input-charset " + charset;
            process.StartWait();
            return process.StandardError.ReadLine().Trim().ToLower() != "unknown charset";
        }
        
        private long CalculateImageSize()
        {
            MkisofsProcess process = new MkisofsProcess();
            process.StartInfo.Arguments = String.Format(
                "-r {0} -q -graft-points -print-size {1}",
                use_utf8 ? "-input-charset utf8" : "",
                graft_paths);
                
            process.StartWait();
            
            if(process.ExitCode != 0) {
                return 0;
            }
            
            string blocks = process.StandardOutput.ReadLine().Trim();
            return Convert.ToInt64(blocks) * 2048;
        }
        
        public void Create(string imagePath)
        {
            use_utf8 = TestCharset("utf8");

            OnImageStatusChanged(ImageStatus.CalculatingSize, 0, 0, null);
            image_size = CalculateImageSize();

            MkisofsProcess process = new MkisofsProcess();
            process.StartInfo.Arguments = String.Format(
                "-r {0} -q -graft-points -o \"{1}\" {2}",
                use_utf8 ? "-input-charset utf8" : "",
                imagePath,
                graft_paths);

            process.Start();

            double last_fraction = 0.0;

            while(!process.HasExited) {
                if(!File.Exists(imagePath)) {
                    continue;
                }

                FileInfo fileinfo = new FileInfo(imagePath);
                long current_size = fileinfo.Length;
                double fraction = current_size / (double)image_size;

                if(fraction - last_fraction > 0.01) {
                    OnImageStatusChanged(ImageStatus.Writing, image_size, current_size, null);
                    last_fraction = fraction;
                }
            }

            if(process.ExitCode != 0) {
                string error = process.StandardError.ReadLine();
                int trim_pos = error.IndexOf("mkisofs: ");

                if(trim_pos > 0) {
                    error = error.Substring(trim_pos + 9);
                }

                error = error.Trim();
                
                OnImageStatusChanged(ImageStatus.Error, 0, 0, error);
                throw new ApplicationException(error);
            }

            OnImageStatusChanged(ImageStatus.Finished, image_size, image_size, null);
        }
        
        protected virtual void OnImageStatusChanged(ImageStatus status, long imageSize, 
            long imageWritten, string message)
        {
            ImageStatusHandler handler = ImageStatusChanged;
            if(handler != null) {
                handler(this, new ImageStatusArgs(status, imageSize, imageWritten, message));
            }
        }
    }
}
