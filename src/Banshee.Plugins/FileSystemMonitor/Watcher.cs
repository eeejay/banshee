using System;
using System.Collections;
using System.Data;
using System.Threading;
using Mono.Unix;

using Banshee.Base;

namespace Banshee.Plugins.FileSystemMonitor
{
    public class Watcher : Banshee.Plugins.Plugin
    {
        private ArrayList toImport;
        private ArrayList toRemove;
        
        private Thread updateThread;
        
        private Watch watch;

        public override string DisplayName { get { return "File System Monitor"; } }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Automatically keep your Banshee library directory in sync " +
                    "with your music folder. This plugin responds to changes made " +
                    "in the file system to reflect them in your library."
                );
            }
        }
        
        public override string [] Authors {
            get {
                return new string [] { 
                    "Do\u011facan G\u00fcney"
                };
            }
        }

        protected override void PluginInitialize()
        {
            toImport = new ArrayList();
            toRemove = new ArrayList();
            
            throw new ApplicationException("This plugin is incomplete and unstable");
        
            updateThread = new Thread(new ThreadStart(Update));
        
            if(Inotify.Enabled) {
                watch = new InotifyWatch(toImport, toRemove, Globals.Library.Location);
            } else {
                watch = new FileSystemWatcherWatch(toImport, toRemove, Globals.Library.Location);
            }
            
            updateThread.Start();
        }
		
		protected override void PluginDispose()
		{
            updateThread.Abort();
            watch.Stop();
		}
        
        private void Update()
        {
            while(true) {
    		    lock(watch) {
                    if(toRemove.Count != 0) {
                        Console.WriteLine("toRemove begin");
                        
                        string query = " FROM TRACKS WHERE";                     
    		    
            		    foreach(string s in toRemove) {
            		        Console.WriteLine(s as string);
                            query +=" Uri LIKE \"file://" + s + "/%\"";
                            query += " OR Uri LIKE \"file://" + s + "\"";
                            
                            query += " OR";                            
                        }
                        
                        Console.WriteLine("toRemove end");
                        
                        query = query.Substring(0, query.Length - 3);
                        
                        string selectQuery = "SELECT TrackID, Uri" + query;
                        string deleteQuery = "DELETE" + query;
                                               
                        IDataReader reader = Globals.Library.Db.Query(selectQuery);
                        while(reader.Read()) {
                            Globals.Library.Remove(Convert.ToInt32(reader[0] as string), 
                                                new System.Uri(reader[1] as string));
                        }                       
                        Globals.Library.Db.Execute(deleteQuery);
                      
                        toRemove.Clear();
                    }
                    
                    if(toImport.Count != 0) {
                        Console.WriteLine("toImport begin");

              		    foreach(string s in toImport) {
                            Console.WriteLine(s);
                            ImportManager.Instance.QueueSource(s);
                            watch.RecurseDirectory(s);
                        }
                        
                        Console.WriteLine("toImport end");
                        
                        toImport.Clear();
                    }
                }
                
                Thread.Sleep(5000);
		    }
		}
    }
}
