using System;

using Hyena;

using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Podcasting
{
    public class PodcastService : IExtensionService, IDisposable
    {
    	private PodcastCore pc;
		
		public string ServiceName
		{
			get { return "PodcastService"; } 
		}
    	
    	public void Initialize ()
    	{
           try {
                pc = new PodcastCore ();
            } catch (Exception e) {
                Console.WriteLine (e.Message);
                Console.WriteLine (e.StackTrace);                        
                throw new ApplicationException ("Unable to initialize PodcastCore.");
            }   
    	}
    	
    	public void Dispose ()
    	{
    	    if (pc != null) {
    	    	pc.Dispose ();
    	    	pc = null;
    	    }
    	    
    	    Log.Debug ("PodcastCore Disposed");
    	}
    }
}