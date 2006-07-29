/***************************************************************************
 *  MtpDapTrackInfo.cs
 *
 *  Copyright (C) 2006 Novell and Patrick van Staveren
 *  Written by Patrick van Staveren (trick@vanstaveren.us)
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
using Banshee.Base;
using Banshee.Dap;
using LibGPhoto2;

namespace Banshee.Dap.Mtp
{
    public sealed class MtpDapTrackInfo : DapTrackInfo
    {
        GPhotoDeviceFile device_file;
        public MtpDapTrackInfo(GPhotoDeviceFile DevFile) : base()
        {
            device_file = DevFile;
            //uri = new SafeUri(String.Format("dap://{0}/{1}", DevFile.Directory, DevFile.Filename));
                        // uri commented out on upgrade to banshee cvs on 2006-04-20 from a 2 month old snapshot - apparently safeuri is causing crashes in transcode in my system.  urgh.
            //uri = new SafeUri(String.Format("dap://{0}/{1}/{2}", dap.Uid, DevFile.Directory, DevFile.Filename));
            album = DevFile.AlbumName;
            artist = DevFile.Artist;
            title = DevFile.Name;
            genre = DevFile.Genre;
            duration = TimeSpan.FromMilliseconds(DevFile.Duration);;
            play_count = DevFile.UseCount;
            track_number = DevFile.Track;
            year = DevFile.Year;
            CanPlay = false;
            this.NeedSync = false;
        }

        public GPhotoDeviceFile DeviceFile {
            get {
                return device_file;
            }
        }
        
        public void MakeFileUri () {
            try {
                byte[] data = device_file.CameraFile.GetDataAndSize();
                string temp_file_string = Path.GetTempFileName();
                File.Delete(temp_file_string);
                FileStream temp_file = File.Open(temp_file_string + device_file.Extension, FileMode.Create);
                temp_file.Write(data, 0, data.Length);
                temp_file.Close();
                uri = new SafeUri("file://" + temp_file.Name);
            } catch (Exception e) {
                LogCore.Instance.PushDebug (String.Format("MTP DAP: Failed to copy track {0} locally for importing.  Exception: {1}", title, e.ToString()), "");
            }
        }
        
        public override void Save() {
            Console.WriteLine("track saving doesn't work.");
            /*GpDeviceFile.AlbumName = album;
            GpDeviceFile.Artist = artist;
            GpDeviceFile.Genre = genre;
            GpDeviceFile.Name = title;
            GpDeviceFile.Track = track_number;
            GpDeviceFile.UseCount = play_count;
            GpDeviceFile.Year = year;
            
            GpDeviceFile.SaveMetadata();
            Console.WriteLine("Track Save just might have worked!");*/
        }

        public override void IncrementPlayCount() {
            play_count++;
            Save();
        }

        protected override void SaveRating() {
            Save();
        }
    }
}
