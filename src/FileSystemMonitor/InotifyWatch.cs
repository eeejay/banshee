using System;
using System.Collections;
using System.IO;

namespace Banshee.FileSystemMonitor
{
    public sealed class InotifyWatch : Watch 
    {
        private bool verbose = false;
                 
        private bool HasFlag(Inotify.EventType type, Inotify.EventType val)
        {
            return (type & val) == val;
        }
    
        public InotifyWatch(ArrayList im, ArrayList rm, string folder) : base(im, rm, folder) 
        {
            RecurseDirectory(PathUtil.FileUriStringToPath(musicFolder));
            Inotify.Verbose = verbose;
            Inotify.Start();
        }
        
        public override bool IsWatching(string path)
        {
            return Inotify.IsWatching(path);
        }
       
        public override bool AddWatch(string path)
        {            
            if(IsWatching(path) || !Directory.Exists(path)) {
                return false;
            }
                
            Console.WriteLine ("Adding watch to {0}", path);
            
            Inotify.Subscribe(path, OnInotifyEvent, Inotify.EventType.CloseWrite | 
                Inotify.EventType.MovedFrom | Inotify.EventType.MovedTo |
                Inotify.EventType.Create | Inotify.EventType.Delete);
               
            return true;
        }
        
        public override void Stop()
        {
            Inotify.Stop();
        }
        
        private void OnInotifyEvent(Inotify.Watch watch, string path, string subitem, 
            string srcpath, Inotify.EventType type)
        {
            Console.WriteLine("Got event ({03}) {0}: {1}/{2}", type, path, subitem, srcpath);

            string fullPath = Path.Combine(path, subitem);

            lock(this) {
                if(HasFlag(type, Inotify.EventType.MovedTo) || HasFlag(type, Inotify.EventType.CloseWrite)) {
                    UniqueAdd(toImport, fullPath);
                        
                    if(srcpath != null) {
                        UniqueAdd(toRemove, srcpath);
                    }
                } else if(HasFlag(type, Inotify.EventType.Create)) { /*HasFlag (type, Inotify.EventType.IsDirectory)) */
                    UniqueAdd(toImport, fullPath);
                } else if(HasFlag(type, Inotify.EventType.Delete) || HasFlag(type, Inotify.EventType.MovedFrom)) {
                    UniqueAdd(toRemove, fullPath);
                }
            }
        }    
    }
}
