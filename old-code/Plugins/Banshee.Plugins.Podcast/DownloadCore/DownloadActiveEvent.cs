// created on 3/31/2006 at 2:59 AM
/*
using Gtk;
 
using Banshee.Base;
using Banshee.Widgets;
 
  private ActiveUserEvent userEvent;
  
  private bool cancel_requested;
  private readonly object ueSync = new object ();  
 
//-----------------------------------------------------------------------
        private void CreateUserEvent()
        {
            lock (ueSync) {
             if(userEvent == null) {
                 userEvent = new ActiveUserEvent(Catalog.GetString("Download"));
                 userEvent.Icon = IconThemeUtils.LoadIcon(22, "system-search", Stock.Network);
                 userEvent.Header = Catalog.GetString("Downloading files");
                 userEvent.Message = Catalog.GetString("Initializing downloads");
                 userEvent.CancelRequested += OnUserEventCancelRequestedHandler;
     cancel_requested = false;
             }
            }
        }
        
        private void DestroyUserEvent()
        {
            lock (ueSync) {        
             if(userEvent != null) {
                 lock(userEvent) {
                     userEvent.Dispose();
                     userEvent = null;
                     cancel_requested = false;
                 }
             }
            }
        }
 
  private void OnDownloadCompleteHandler (object sender, DownloadTaskFinishedEventArgs args)
  {
   Console.WriteLine ("OnDownloadCompleteHandler");  
         TrackInfo lti;
         DownloadInfo dif = args.Info;
         Console.WriteLine ("FILE URI:  {0}", dif.FileName);
         Console.WriteLine ("FILE URI:  {0}", dif.FileName);
         
         try {
          lti = new LibraryTrackInfo(dif.FileName);
          } catch(ApplicationException) {
           lti = Globals.Library.TracksFnKeyed[Library.MakeFilenameKey(new Uri (dif.FileName))] as TrackInfo;
          }
               
          if(lti != null) {                       
           HaveTrackInfoHandler handler = HaveTrackInfo;
          if(handler != null) {
           HaveTrackInfoArgs trackArgs = new HaveTrackInfoArgs();
           trackArgs.TrackInfo = lti;
           handler(this, trackArgs);
       }
         }
  }
  
                        
  private void OnStatusUpdatedHandler (object sender, StatusUpdatedEventArgs args)
  {
            string message;
            string disp_progress;
            double progress = (double) args.Progress / 100;
                        
      if (args.FailedDownloads <= 0) {
    disp_progress = String.Format (
     Catalog.GetString("Downloading Files ({0} of {1} completed)"), 
     args.DownloadsComplete, args.TotalDownloads);
   } else {
    disp_progress = String.Format (
     Catalog.GetString("Downloading Files ({0} of {1} completed)\n{2} failed"), 
     args.DownloadsComplete, args.TotalDownloads, args.FailedDownloads);    
   }
 
      if (args.CurrentDownloads == 1) {
    message = String.Format (Catalog.GetString("Currently transfering 1 file at {0} KB/s"), 
     args.Speed);
   } else {
    message = String.Format (Catalog.GetString("Currently transfering {0} files at {1} KB/s"), 
     args.CurrentDownloads, args.Speed);    
   }
      
   lock (ueSync) {
    if (userEvent != null) {
              if (!cancel_requested) {
               userEvent.Header = disp_progress;
               userEvent.Message = message;
               userEvent.Progress = progress;
              }            
             }      
   }                
  }
  
  private void OnDownloadTaskFinishedHandler (object sender, EventArgs args)
  {
   Thread.Sleep (1000);  
   DestroyUserEvent ();
  }
                                                    
        private void OnUserEventCancelRequestedHandler (object sender, EventArgs args)
        {
            lock (ueSync) {
             if(userEvent != null) {
     cancel_requested = true;             
              userEvent.CanCancel = false;
              userEvent.Progress = 0.0;
              userEvent.Header = "Canceling Downloads";
              userEvent.Message = "Waiting for downloads to terminate"; 
 
     ThreadAssist.Spawn(new ThreadStart(CancelAll));
             }
            }
        }  
//-----------------------------------------------------------------------
*/
