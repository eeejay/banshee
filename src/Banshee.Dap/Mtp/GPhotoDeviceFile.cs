/***************************************************************************
 *  GPhotoDeviceFile.cs
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
using System.Collections;
using System.Text;
using LibGPhoto2;
using Banshee.Base;

namespace Banshee.Dap.Mtp{

public class GPhotoDeviceFile
{
    public GPhotoDevice Dev;
    
    public CameraFile CameraFile;
    private string directory;
    private string filename;
    
    private string extension;
    
    public string Artist;
    public double Duration;
    public uint Track;
    public string Genre;
    public int Year;
    public string AlbumName;
    public string Name;
    public uint UseCount;
    
    /* new constructor for adding tracks in Synchronize()
    / allows for full internal checking for bad characters, and a self constructed path*/
    public GPhotoDeviceFile (SafeUri uri, GPhotoDevice device)
    {
        Dev = device;
        CameraFile = new CameraFile();
        CameraFile.Open(uri.LocalPath);
        extension = Path.GetExtension(uri.LocalPath);
        filename = null;
        directory = null;
    }

    public GPhotoDeviceFile (string dir, string file, string meta, GPhotoDevice device)
    {
        Dev = device;
        directory = dir;
        filename = file;
        CameraFile = null;
        Metadata = meta;
    }

    // old constructor for sending tracks from Sync
/*  public GPhotoDeviceFile (string dir, string file, SafeUri uri, GPhotoDevice device) 
    {
        // FIXME: could this be where we're crashing with cvs?
        Dev = device;
        directory = dir;
        filename = file;
        CameraFile = new CameraFile();
        CameraFile.Open(uri.LocalPath);
        CameraFile.SetName(Filename);
    }*/

    public void GenerateProperPath() {
        directory = Dev.Store + "Music/" + GetValidName(Artist) + "/" + GetValidName(AlbumName);
        filename = GetValidName(String.Format("{0}. {1}{2}", Track, Name, extension));
        CameraFile.SetName(filename);
        Console.WriteLine("proper path: dir={0} file={1}", directory, filename);
    }        

    private string GetValidName(string input) {
        string output = "";

        for(int i = 0; i < input.Length; i++) {
            if(input[i] == '/' || input[i] == '\\' || input[i] == ':' || input[i] == '?') {
                output = output + '_';
            } else {
                if(output == "")
                    output = input[i].ToString().ToUpper();
                else
                    output = output + input[i];
            }
        }
        return output;
    }

    public string Filename {
        get {
            return filename;
        }
        set {
            filename = value;
        }
    }

    public string Directory {
        get {
            return directory;
        }
        set {
            directory = value;
        }
    }

    private string LookupMetaValue(string meta, string tag, bool isNumeric)
    {
        int loc_start = meta.IndexOf("<" + tag + ">");
        int loc_end   = meta.IndexOf("</" + tag + ">");
        if(loc_start <= 0 || loc_end <= 0){
            if(isNumeric)
                return "0000";
            return "";
        }
        int start = loc_start + 1 + tag.Length + 1;
        string metaValue = meta.Substring(start, loc_end - start);
        if(metaValue == "(null)" && isNumeric)
            metaValue = "0000";
        return(metaValue);
    }

    public string Metadata {
        get {
            return(String.Format("<Duration>{0}</Duration>\n", Duration) +
                String.Format("<Artist>{0}</Artist>\n", (Artist == "" || Artist == null) ? "Unknown" : Artist) + 
                String.Format("<AlbumName>{0}</AlbumName>\n", (AlbumName == null)   ? "" : AlbumName) +
                String.Format("<Name>{0}</Name>\n", (Name == "" || Name == null)     ? "Unknown" : Name) + 
                String.Format("<Track>{0}</Track>\n", Track) + 
                String.Format("<Genre>{0}</Genre>\n", Genre) + 
                String.Format("<UseCount>{0}</UseCount>\n", UseCount) + 
                String.Format("<OriginalReleaseDate>{0:0000}0101T0000.0</OriginalReleaseDate>", Year) +
                String.Format("<ObjectFileName>{0}</ObjectFileName>", Filename)
            );
        }
        set {
            Artist = LookupMetaValue(value, "Artist", false);
            Duration = double.Parse(LookupMetaValue(value, "Duration", true));
            Track = uint.Parse(LookupMetaValue(value, "Track", true));
            Genre = LookupMetaValue(value, "Genre", false);
            Year = int.Parse(LookupMetaValue(value, "OriginalReleaseDate", true).Substring(0, 4));
            AlbumName = LookupMetaValue(value, "AlbumName", false);
            Name = LookupMetaValue(value, "Name", false);
            UseCount = uint.Parse(LookupMetaValue(value, "UseCount", true));
        }
    }

    public void SaveMetadata()
    {
        Dev.PutMetadata(this);
    }

    ~GPhotoDeviceFile () {
        Dispose();
    }
    
    public void Dispose () {
        if (CameraFile != null) 
            CameraFile.Dispose ();

        Dev = null;
    }
}

}
