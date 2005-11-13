using System;
using System.Collections;
using System.Data;
using System.Threading;

namespace Banshee.FileSystemMonitor
{
    public class Watcher : IDisposable
    {
        private ArrayList toImport;
        private ArrayList toRemove;
        
        private Thread updateThread;
        
        private Watch watch;
        
        public Watcher(string watchDirectory)
        {
            toImport = new ArrayList();
            toRemove = new ArrayList();
        
            updateThread = new Thread(new ThreadStart(Update));
        
            if(Inotify.Enabled) {
                Console.WriteLine("The power of inotify!");
                watch = new InotifyWatch(toImport, toRemove, watchDirectory);
            } else {
                watch = new FileSystemWatcherWatch(toImport, toRemove, watchDirectory);
            }
            
            updateThread.Start();
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
                                               
                        IDataReader reader = Core.Library.Db.Query(selectQuery);
                        while(reader.Read()) {
                            Core.Library.Remove(Convert.ToInt32(reader[0] as string), 
                                                new System.Uri(reader[1] as string));
                        }                       
                        Core.Library.Db.Execute(deleteQuery);
                      
                        toRemove.Clear();
                    }
                    
                    if(toImport.Count != 0) {
                        FileLoadTransaction transaction = 
                            new FileLoadTransaction(null, true, true);
                            
                        Console.WriteLine("toImport begin");

              		    foreach(string s in toImport) {
                            Console.WriteLine(s);
                            transaction.AddPath(s);
                            watch.RecurseDirectory(s);
                        }
                        
                        Console.WriteLine("toImport end");
                        
                        transaction.Register();
                        
                        toImport.Clear();
                    }
                }
                
                Thread.Sleep(5000);
		    }
		}
		
		public void Dispose()
		{
            updateThread.Abort();
            watch.Stop();
		}
    }
}
