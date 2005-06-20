/***************************************************************************
 *  GstMetadata.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.IO;
using System.Data;
using System.Collections;
using System.Threading;
using Gst;

namespace Sonance
{
	public class ThreadedGstMetadataLoader
	{
		private System.Threading.Thread thread;
		private GstMetadata md;
		private string path;
		
		public ThreadedGstMetadataLoader(string path)
		{
			this.path = path;
			
			thread = new System.Threading.Thread(new ThreadStart(Start));
			thread.Start();
			thread.Join(new TimeSpan(0, 0, 5));
			
			if(thread.IsAlive) {
				try {
					thread.Abort();
				} catch(Exception) {}
				
				throw new SonanceException("GstMetadata timed out trying to iterate: " + path);
			}
		}
		
		private void Start()
		{
			try {
				md = new GstMetadata(path);
			} catch(Exception) {
				md = null;
			}
		}
		
		public GstMetadata Metadata 
		{
			get {
				return md;
			}
		}
	}

	public class GstMetadata
	{
	    private bool handoff = false;
	    
	    private Hashtable tagTable;
	    
	    private string uri;
	    private string mimetype;
	    private long duration;

	    public GstMetadata(string uri)
	    {
			if(uri == null) 
				return;

			this.uri = uri;

			tagTable = new Hashtable();

			Bin pipeline = null;
			pipeline = new Pipeline("pipeline");
	        pipeline.FoundTag += new FoundTagHandler(HandleTagFound);
	    
			Element filesrc = ElementFactory.Make("gnomevfssrc", "gnomevfssrc");
			if(filesrc == null)
				throw new SonanceException("gnomevfssrc object couldn't be created");
			
			filesrc.SetProperty("location", uri);
	    
			TypeFindElement typefind = 
	            (TypeFindElement)ElementFactory.Make("typefind", "typefind");
			if(typefind == null)
				throw new SonanceException("typefind object couldn't be created!");
	        
	        typefind.HaveType += new HaveTypeHandler(HandleHaveType);        
			
			Element decoder = ElementFactory.Make("spider", "spider");
			if(decoder == null) 
				throw new SonanceException("decoder object couldn't be created!");
			
			FakeSink sink = (FakeSink)ElementFactory.Make("fakesink", "sink");
			if(sink == null)
				throw new SonanceException("sink object couldn't be created!");
			
	        sink.SignalHandoffs = true;
	        sink.Handoff += new HandoffHandler(HandleHandoff);

	        pipeline.Add(filesrc);
	        pipeline.Add(typefind);
	        pipeline.Add(decoder);
	        pipeline.Add(sink);

	        filesrc.Link(typefind);
	        typefind.Link(decoder);

	        Gst.Caps filtercaps = Gst.Caps.FromString("audio/x-raw-int");
	        decoder.LinkFiltered(sink, ref filtercaps);

	        pipeline.SetState(ElementState.Playing);

	        while(pipeline.Iterate() && !handoff);

	        int format = (int)Gst.Format.Time;
	        if(sink.Query(Gst.QueryType.Total, ref format, out duration))
	            duration = duration / (1000 * 1000 * 1000);
	        
	        pipeline.SetState(ElementState.Null);
	        pipeline = null;
	    }

	    private void HandleHandoff(object o, HandoffArgs args)
	    {
	        handoff = true;
	    }

	    private void HandleHaveType(object o, HaveTypeArgs args)
	    {
	        Caps caps = args.Caps;
	        mimetype = caps.GetStructure(0).Name;
	    }

	    private void HandleTagFound(object o, FoundTagArgs args)
	    {
	        TagList tags = args.TagList;
	        tags.Foreach(new TagForeachFunc(ProcessTag));
	    }

	    private void ProcessTag(TagList list, string tag)
	    {
	    	string Str = null;
	    	uint Uint = 0;
	    	double Dbl = 0.0;
	    
	    	switch(tag) {
	    		case CommonTags.Artist:
	    		case CommonTags.Album:
	    		case CommonTags.Title:
	    		case CommonTags.Genre:
	    		case CommonTags.Performer:
	    			list.GetString(tag, out Str);
	    			tagTable.Add(tag, Str);
	    			break;
	    		case CommonTags.TrackNumber:
	    		case CommonTags.TrackCount:		
	    		case CommonTags.Date:	
	    			list.GetUint(tag, out Uint);
	    			tagTable.Add(tag, Uint);
	    			break;
	    		case CommonTags.TrackPeak:
	    		case CommonTags.TrackGain:
	    		case CommonTags.AlbumPeak:
	    		case CommonTags.AlbumGain:
	    			list.GetDouble(tag, out Dbl);
	    			tagTable.Add(tag, Dbl);
	    			break;
	    	}
	    }
	    
	    private string GetTagString(string tag)
	    {
	    	object o = tagTable[tag];
	    	if(o == null)
	    		return null;
	    		
	    	return ((string)o).Trim();
	    }
	    
	    private uint GetTagUint(string tag)
	    {
	    	object o = tagTable[tag];
	    	if(o == null)
	    		return 0;
	    		
	    	return (uint)o;
	    }
	    
	    private double GetTagDouble(string tag)
	    {
	    	object o = tagTable[tag];
	    	if(o == null)
	    		return 0.0;
	    	
	    	return (double)o;
	    }
	    
	    public string Uri       { get { return uri;         } } 
	   	public string MimeType  { get { return mimetype;    } }
	    
	    public string Artist    { get { return GetTagString(CommonTags.Artist);    } }
	    public string Album     { get { return GetTagString(CommonTags.Album);     } }
	    public string Title     { get { return GetTagString(CommonTags.Title);     } }
	    public string Genre     { get { return GetTagString(CommonTags.Genre);     } }
	    public string Performer { get { return GetTagString(CommonTags.Performer); } }
	    
	    public long Duration    { get { return duration;    } }
	   	public uint TrackNumber { get { return GetTagUint(CommonTags.TrackNumber); } }
	    public uint TrackCount  { get { return GetTagUint(CommonTags.TrackCount);  } }
	    
	    public double TrackGain { get { return GetTagDouble(CommonTags.TrackGain); } }
	    public double TrackPeak { get { return GetTagDouble(CommonTags.TrackPeak); } }
	    public double AlbumGain { get { return GetTagDouble(CommonTags.AlbumGain); } }
	    public double AlbumPeak { get { return GetTagDouble(CommonTags.AlbumPeak); } }
	    
	    public DateTime Date
	    { 
	    	get { 
	    		uint date = GetTagUint(CommonTags.Date);
	    		return new DateTime(date);
	    	}
	    }
	    
	    public bool IsAcceptedType 
	    {
	    	get {
	    		if(mimetype == null)
	    			return false;
	    			
	    		return Core.Instance.DecoderRegistry.SupportedMimeType(mimetype);
	    	}
	    }
	}
}
