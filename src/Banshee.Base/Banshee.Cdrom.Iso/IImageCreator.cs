/***************************************************************************
 *  IImageCreator.cs
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

namespace Banshee.Cdrom.Iso
{
    public interface IImageCreator
    {
        event ImageStatusHandler ImageStatusChanged;
        void AddPath(string layoutPath, string path);
        void Create(string imagePath);
    }
    
    public delegate void ImageStatusHandler(object o, ImageStatusArgs args);
    
    public sealed class ImageStatusArgs : EventArgs
    {
        private ImageStatus status;
        private long image_size;
        private long image_written;
        private string message;
        
        public ImageStatusArgs(ImageStatus status, long imageSize, long imageWritten, string message)
        {
            this.status = status;
            this.image_size = imageSize;
            this.image_written = imageWritten;
            this.message = message;
        }
        
        public ImageStatus Status {
            get { return status; }
        }
        
        public long ImageSize {
            get { return image_size; }
        }
        
        public long ImageWritten {
            get { return image_written; }
        }
        
        public double Progress {
            get { return image_size == 0 ? 0 : image_written / (double)image_size; }
        }
        
        public string ErrorMessage {
            get { return message; }
        }
    }
}
