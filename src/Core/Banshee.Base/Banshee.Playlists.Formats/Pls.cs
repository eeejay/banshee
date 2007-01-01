using System;
//using System.Collections;
using System.Collections.Generic;
using System.IO;

using Mono.Unix;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Playlists;

namespace Banshee.Playlists.Formats
{    
    public class Pls : PlaylistFile
    {    
        private const string file_header = "[playlist]";
        
        public Pls()
        {
        }
        
        public override string Extension
        {
            get { return "pls"; }
        }
        
        public override string Name
        {
            get { return Catalog.GetString("Shoutcast Playlist version 2 (*.pls)"); }            
        }
                
        public override void Export(string uri, Source source)
        {
            // Example output:
            //[playlist]
            //File1=C:\My Music\Pink Floyd\1979---The_Wall_CD1\1.In_The_Flesh.mp3
            //Title1=Pink Floyd - In The Flesh
            //Length1=199
            //File2=C:\My Music\Pink Floyd\1979---The_Wall_CD1\10.One_Of_My_Turns.mp3
            //Title2=Pink Floyd - One Of My Turns
            //Length2=217
            //NumberOfEntries=2
            //Version=2

            try {
                bool use_relative_paths = true;
                string path = UpdateExtension(uri);
                string save_directory = Path.GetDirectoryName(path);        
                
                if (save_directory == null) {
                    use_relative_paths = false;
                }
                
                TextWriter tw = new StreamWriter(path);
                tw.WriteLine(file_header);
            
                string trackPath = null;
                int trackNumber = 0;                
                foreach (TrackInfo ti in source.Tracks) {
                    trackNumber++;
                    
                    if (use_relative_paths) {
                        trackPath = AbsoluteToRelative(ti.Uri.AbsolutePath, save_directory);
                    } else {
                        trackPath = ti.Uri.AbsolutePath;
                    }
                    
                    tw.WriteLine("File" + trackNumber + "=" + trackPath);
                    tw.WriteLine("Title" + trackNumber + "=" + GetTitle(ti));
                    tw.WriteLine("Length" + trackNumber + "=" + ((int) ti.Duration.TotalSeconds));
                }
                                
                tw.WriteLine("NumberOfEntries=" + trackNumber);
                tw.WriteLine("Version=2");
                                
                tw.Close();
            } catch (Exception e) {
                LogCore.Instance.PushError(Catalog.GetString("Exception: "), e.Message);
            }
        }
        
        public override string[] Import(string uri)
        {        
            List<string> list = new List<string>();            
            TextReader tr = null;
            try {
                tr = new StreamReader(uri);
                                
                bool validFile = false;                
                string line = null;
                while ((line = tr.ReadLine()) != null) {
                    line = line.Trim();
                    if (line.Length == 0) {
                        continue;
                    }
                                                            
                    if (line.StartsWith("File")) {                    
                        int equals = line.IndexOf("=");
                        
                        if (equals != -1) {
                            line = line.Remove(0, (equals + 1));                            
                            
                            string fullPath = IsValidFile(uri, line);
                               if (fullPath != null) {    
                                list.Add(fullPath);
                                validFile = true;
                            }                            
                        }                        
                    }
                }
                
                /*if (!validFile) {
                    throw new InvalidPlaylistException(Catalog.GetString("Not a valid PLS file."));
                }*/
            
            } finally {
                if (tr != null) {                    
                    tr.Close();
                }
            }
            
            return list.ToArray();
        }
    }
}
