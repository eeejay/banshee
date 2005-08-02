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
		
		player.OpenUri(uri);
		player.Volume = 80;
		player.Play();

		while(player.Iterate());
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
