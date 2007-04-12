using System;
using System.IO;

using Banshee.Base;
using Banshee.Sources;

namespace Banshee.Playlists.Formats
{    
    public abstract class PlaylistFile
    {            
        public PlaylistFile() 
        {            
        }
        
        public abstract string Extension
        {
            get;
        }
        
        public abstract string Name
        {
            get;            
        }
        
        public abstract void Export(string uri, Source source);
        public abstract string[] Import(string uri);
        
        protected string GetTitle(TrackInfo ti)
        {
            string title = ti.Title;
            
            if (title == null) {
                title = "";
            } else {
                title = title.Trim();
            }
            
            if (title.Length != 0) {
                return title;
            } else {
                return System.IO.Path.GetFileName(ti.Uri.AbsolutePath);                
            }            
        }    
        
        public string UpdateExtension(string uri) 
        {            
            if (uri == null) {
                return null;
            }
            
            string extension = Path.GetExtension(uri).ToLower();            
            
            if (!Path.HasExtension(uri)) {
                uri += "." + Extension;
            } else if (!extension.Equals(Extension.ToLower())) {
                uri = Path.ChangeExtension(uri, Extension);
            }
            
            return uri;

        }
        
        protected string GetAbsolutePath(string uri, string relativeTo)
        {
            if (!Path.IsPathRooted(uri)) {
                uri = Path.GetFullPath(relativeTo + Path.DirectorySeparatorChar + uri);
            }
            return uri;
        }
        
        protected string AbsoluteToRelative(string absPath, string relativeTo)
        {
            string relativePath = null;    
            char[] slash = new char[] { Path.DirectorySeparatorChar };
            
            try {
                // Determine the common part of the path.
                string commonPath = Path.DirectorySeparatorChar.ToString();
                string[] tokens = absPath.Split(slash, StringSplitOptions.RemoveEmptyEntries);                
                for (int i = 0; i < tokens.Length; i++) {
                    string temp = commonPath + tokens[i];    
                    if (relativeTo.StartsWith(temp)) {                        
                        commonPath = temp + Path.DirectorySeparatorChar;
                    } else {
                        break;
                    }
                }
                                
                // Remove the common part of the paths.
                relativePath = absPath.Remove(0, commonPath.Length);
                string tempRelativeTo = relativeTo.Remove(0, commonPath.Length - 1);
                
                // Add code to walk up the path if necessary.
                tokens = tempRelativeTo.Split(slash, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tokens.Length; i++) {
                    relativePath = ".." + Path.DirectorySeparatorChar + relativePath;
                }            
            } catch (Exception) {
                // Ignore exceptions, just return the absolute path
                // if we run into a problem.
                relativePath = absPath;                
            }
            
            return relativePath;
        }
        
        protected string IsValidFile(string playlistUri, string line)
        {
            bool valid = false;
            string fullPath = null;
            
            // Test to see if line is a normal path that exists.
            // For example: "/home/tale/RATM/Bombtrack.mp3"
            fullPath = GetAbsolutePath(line, Path.GetDirectoryName(playlistUri));
            if (File.Exists(fullPath)) {
                valid = true;
            }
            
            if (!valid) {
                // If not valid, then the line may be encoded as a URI.
                // In this case, convert it a regular path and test to see if
                // it is valid again.
                // For example: "file:///home/tale/RATM/Bombtrack.mp3"                
                try {
                    SafeUri safeUri = new SafeUri(line);
                    if (safeUri.IsLocalPath) {
                        line = SafeUri.UriToFilename(safeUri.AbsoluteUri);
                        fullPath = GetAbsolutePath(line, Path.GetDirectoryName(playlistUri));
                        if (File.Exists(fullPath)) {
                            valid = true;
                        }
                    }
                } catch (Exception) {
                    // Ignore exceptions, continue.
                }
            }
            
            return valid ? fullPath : null;
        }
    }
}
