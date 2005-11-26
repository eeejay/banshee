/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
using System;
using Helix;

public class HxPlayerTest
{
	public static void Main(string [] args)
	{
		string uri = "file:///home/aaron/music/12 Stones/12 Stones/12 Stones - My Life.ogg";
		HxPlayer player = new HxPlayer();
		
		player.LengthChanged += OnPlayerLengthChanged;
		player.VolumeChanged += OnPlayerVolumeChanged;

		Console.WriteLine("Plugins Loaded: " + player.Plugins.Length);

        int index = 0;
        foreach(HxPlugin plugin in player.Plugins) {
            HxFileFormatInfo formatInfo = plugin.FileFormatInfo;
            index++;
            
            if(formatInfo == null)
                continue;
            
            Console.WriteLine(index + ": ");
            
            
            if(formatInfo.MimeTypes != null) {
                foreach(string mime in formatInfo.MimeTypes) {
                    Console.WriteLine("    " + mime);
                }
            }
            
            Console.WriteLine("    ------");
            if(formatInfo.Extensions != null) {
                foreach(string ext in formatInfo.Extensions) {
                    Console.WriteLine("    " + ext);
                }
            }
            
            Console.WriteLine("    ------");
            if(formatInfo.OpenNames != null) {
                foreach(string name in formatInfo.OpenNames) {
                    Console.WriteLine("    " + name);
                }
            }
        }

		/*player.OpenUri(uri);
		player.Volume = 80;
		player.Play();

		while(true)
			player.Iterate();*/
	}
	
	public static void OnPlayerLengthChanged(object o, LengthChangedArgs args)
	{
		Console.WriteLine("LENGTH: " + args.Length);
	}
	
	public static void OnPlayerVolumeChanged(object o, VolumeChangedArgs args)
	{
		Console.WriteLine("VOLUME: " + args.Volume);
	}
}
