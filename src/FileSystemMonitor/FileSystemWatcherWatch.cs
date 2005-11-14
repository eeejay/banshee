using System;
using System.Collections;
using System.IO;

namespace Banshee.FileSystemMonitor
{
    public sealed class FileSystemWatcherWatch : Watch
    {
        private Hashtable watchMap;
        private bool verbose = false;
        
        public FileSystemWatcherWatch(ArrayList im, ArrayList rm, string folder) : base(im, rm, folder)
        {
            watchMap = new Hashtable();
            RecurseDirectory(PathUtil.FileUriStringToPath(musicFolder));
        }
                
        public override bool IsWatching(string path)
        {
            return watchMap.ContainsKey(path);
        }
        
        public override bool AddWatch(string path)
        {
           if(IsWatching(path) || !Directory.Exists(path))
                return false;
                
            if(verbose)
               Console.WriteLine ("Adding watch to {0}", path);
            
            FileSystemWatcher watcher = new FileSystemWatcher();
            
            watcher.Path = path;
            watcher.NotifyFilter = NotifyFilters.LastWrite 
                | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.IncludeSubdirectories = false;
            watcher.Filter = "";

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnDeleted);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            watcher.EnableRaisingEvents = true;
            
            watchMap.Add(path, watcher);
            
            return true;
        }
        
        public override void Stop()
        {
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            lock(this) {
                UniqueAdd(toImport, e.FullPath);
            }
        }
        
        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            lock(this) {
                UniqueAdd(toRemove, e.FullPath);
            }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            lock(this) {
                UniqueAdd(toImport, e.FullPath);
                UniqueAdd(toRemove, e.OldFullPath);
            }
        }
    }
}
