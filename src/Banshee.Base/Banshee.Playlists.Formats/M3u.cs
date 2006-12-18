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
    public class M3u : PlaylistFile
    {    
        private const string file_header = "#EXTM3U";
        
        public M3u()
        {
        }
        
        public override string Extension
        {
            get { return "m3u"; }
        }
        
        public override string Name
        {
            get { return Catalog.GetString("MPEG Version 3.0 Extended (*.m3u)"); }
        }
                
        public override void Export(string uri, Source source)
        {
            // Example output:
            //    #EXTM3U
            //    #EXTINF:111,3rd Bass - Al z A-B-Cee z
            //    mp3/3rd Bass/3rd bass - Al z A-B-Cee z.mp3

            try {
                bool use_relative_paths = true;
                string path = UpdateExtension(uri);
                string save_directory = Path.GetDirectoryName(path);
                
                if (save_directory == null) {
                    use_relative_paths = false;
                }
                
                TextWriter tw = new StreamWriter(path);
                tw.WriteLine(file_header);
            
                foreach (TrackInfo ti in source.Tracks) {                            
                    tw.WriteLine("#EXTINF:" + ((int) ti.Duration.TotalSeconds) + "," + GetTitle(ti));
                    if (use_relative_paths) {
                        tw.WriteLine(AbsoluteToRelative(ti.Uri.AbsolutePath, save_directory));
                    } else {
                        tw.WriteLine(ti.Uri.AbsolutePath);
                    }
                }
                                
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
                    
                    string fullPath = IsValidFile(uri, line);
                       if (fullPath != null) {    
                           list.Add(fullPath);
                           validFile = true;
                       }
                }
                
                /*if (!validFile) {
                    throw new InvalidPlaylistException(Catalog.GetString("Not a valid M3U file."));
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
