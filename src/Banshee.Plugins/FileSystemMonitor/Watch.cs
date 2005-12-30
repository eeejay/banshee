using System;
using System.Collections;
using System.Globalization;
using System.IO;

namespace Banshee.Plugins.FileSystemMonitor
{
    public abstract class Watch 
    {
        protected string musicFolder;
        
        protected ArrayList toImport;
        protected ArrayList toRemove;
        
        public Watch(ArrayList im, ArrayList rm, string folder)
        {
            musicFolder = folder;
            
            toImport = im;
            toRemove = rm;
        }
        
        public bool RecurseDirectory(string path)
        {
            DirectoryInfo di;
    		
            try {
                di = new DirectoryInfo(path);
            } catch(Exception) {
                return false;
            }
    		
            if(!di.Exists)
                return false;
    		
    		if(!AddWatch(path))
                return false;
    		
            foreach(DirectoryInfo sdi in di.GetDirectories()) {
                if(!sdi.Name.StartsWith(".")) 
                    RecurseDirectory(path + "/" + sdi.Name);
            }
            return true;
        }
                                   
        protected void UniqueAdd(ArrayList aList, string item)
		{
            string cur;
            bool added = false;
            
            for(int i = 0; i < aList.Count; i++) {
                cur = aList[i] as string;
            
                if(IsSubfolder(item, cur))
                    return;
                
                if(IsSubfolder(cur, item)) {
                    aList[i] = item;
                    added = true;
                }
            }
            
            if(!added)
                aList.Add(item);
		}
		
		protected bool IsSubfolder(string f1, string f2)
		{
            CompareInfo comp =  CultureInfo.InvariantCulture.CompareInfo;

            return comp.IsPrefix(f1, f2);
		}
		
		public abstract bool IsWatching(string path);
        
        public abstract bool AddWatch(string path);
        
        public abstract void Stop();
    }
}
