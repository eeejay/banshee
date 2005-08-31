/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;

/*public class CddbTest
{
	public static void Main(string [] args)
	{
		if(args.Length < 1) {
			Console.WriteLine("CddbClient <diskid>\n");
			return;
		}
	
		CddbClient cddb = new CddbClient("Banshee", "1.0");
		CddbDisc [] discs = cddb.QueryAll(args[0]);
		
		if(discs == null) {
			Console.WriteLine("NO RESULTS");
		} else {
			foreach(CddbDisc disc in discs) {
				Console.WriteLine("---- MATCH [" + disc.LookupGenre + "] ----");
			
				Console.WriteLine("Artist: " + disc.Artist);
				Console.WriteLine("Title:  " + disc.Title);
				Console.WriteLine("Genre:  " + disc.Genre);
				Console.WriteLine("Year:   " + disc.Year);
				Console.WriteLine("Titles: ");
				foreach(CddbArtistTitle title in disc.Titles)
					Console.WriteLine("  " + title.Artist + " / " 
						+ title.Title);
			}
		}
	}
}*/

public class CddbDisc
{
	public string DiskId;
	public string Artist;
	public string Title;
	public string Genre;
	public int Year;
	public string LookupGenre;
	public CddbArtistTitle [] Titles;
}

public class CddbArtistTitle
{
	public string Artist;
	public string Title;
	
	public CddbArtistTitle(string cddbField, string defArtist)
	{
		string [] parts = System.Text.RegularExpressions.Regex.Split(
			cddbField, " / ");

		if(parts == null || parts.Length <= 1) {
			Artist = defArtist;
			Title = cddbField;
		} else {
			Artist = parts[0].Trim();
			Title = parts[1].Trim();
		}
	}
}

public class CddbClient
{
	private string user = "user";
	private string host = "localhost";
	private string appname;
	private string appversion;
	private string cgi = "http://freedb.freedb.org/~cddb/cddb.cgi";

	public CddbClient(string appname, string appversion)
	{
		this.appname = appname;
		this.appversion = appversion;
	}
	
	public string Cgi 
	{
		set {
			cgi = value;
		}
	}
	
	public string HttpQuery(string postCommand)
	{
		ASCIIEncoding encoding = new ASCIIEncoding();
		byte [] data = encoding.GetBytes(postCommand);
			
			try {
			HttpWebRequest request = WebRequest.Create(cgi) as HttpWebRequest;
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = data.Length;
			
			Stream ws = request.GetRequestStream();
			ws.Write(data, 0, data.Length);
			ws.Close();
			
			HttpWebResponse response = request.GetResponse() as HttpWebResponse;
			StreamReader rs = new StreamReader(response.GetResponseStream());
			string result = rs.ReadToEnd();
			rs.Close();
			
			return result;
		} catch(Exception) {
			return null;
		}
	}
	
	public CddbDisc Query(string discId, string genre)
	{
		string command = String.Format(
			"cmd=cddb+read+{0}+{1}&hello={2}+{3}+{4}+{5}&proto=6",
			genre, discId, user, host, appname, appversion);
		
		return ParseResult(HttpQuery(command), genre);
	}
	
	public CddbDisc [] QueryAll(string discId)
	{
		string [] genres = {
			"blues",
			"classical",
			"country",
			"data",
			"folk",
			"jazz",
			"misc",
			"newage",
			"reggae",
			"rock",
			"soundtrack"
		};
			
		ArrayList results = new ArrayList();
		
		foreach(string genre in genres) {
			CddbDisc disc = Query(discId, genre);
			
			if(disc != null)
				results.Add(disc);
		}
	
		return results.ToArray(typeof(CddbDisc)) as CddbDisc [];
	}
	
	private CddbDisc ParseResult(string result, string lookupGenre)
	{
		if(result == null)
			return null;
		
		string cddbResult = result.Trim();
		int resultCode = 0;
		
		try {
			resultCode = Convert.ToInt32(cddbResult.Substring(0, 3));
		} catch(Exception) {
			resultCode = 0;
		}
		
		if(resultCode != 210)
			return null;
			
		string [] lines = cddbResult.Split('\n');
		
		if(lines == null || lines.Length <= 1)
			return null;
			
		CddbDisc disc = new CddbDisc();
		ArrayList ttitles = new ArrayList();
		
		disc.LookupGenre = lookupGenre;
			
		for(int i = 1; i < lines.Length; i++) {
			lines[i] = lines[i].Trim();
			
			if(lines[i].StartsWith("#") || lines[i] == ".")
				continue;
			
			char [] tok = {'='};
			string [] parts = lines[i].Split(tok, 2);
			
			if(parts.Length != 2)
				continue;
				
			string key = parts[0].Trim();
			string val = parts[1].Trim();
				
			switch(key) {
				case "DTITLE":
					CddbArtistTitle at = new CddbArtistTitle(val, null);
					disc.Artist = at.Artist;
					disc.Title = at.Title;
					break;
				case "DGENRE":
					disc.Genre = val;
					break;
				case "DYEAR":
					try {
						disc.Year = Convert.ToInt32(val);
					} catch(Exception) {}
					break;
			}
			
			if(!key.StartsWith("TTITLE"))
				continue;
				
			ttitles.Add(lines[i]);
		}
			
		CddbArtistTitle [] pre_titles = new CddbArtistTitle[ttitles.Count];
		
		foreach(string line in ttitles) {
			char [] tok = {'='};
			string [] parts = line.Split(tok, 2);
			int index = -1;
			
			try {
				index = Convert.ToInt32(parts[0].Trim().Substring(6));
			} catch(Exception) {
				continue;
			}
		
			pre_titles[index] = new CddbArtistTitle(parts[1].Trim(),
				disc.Artist);	
		}
		
		ArrayList list = new ArrayList();
		for(int i = 0; i < pre_titles.Length; i++) {
			if(pre_titles[i] == null)
				continue;
				
			list.Add(pre_titles[i]);
		}
		
		disc.Titles = list.ToArray(typeof(CddbArtistTitle)) 
			as CddbArtistTitle [];
			
		return disc;
	}
}

