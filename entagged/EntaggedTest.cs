using System;
using System.IO;
using Entagged;

public class EntaggedTest
{
	public static void Main(string [] args)
	{
		foreach(string file in Directory.GetFiles("/home/aaron/Music/F02")) {
			AudioFileWrapper af = new AudioFileWrapper(file);
			Console.WriteLine(af.Artist + " - " + af.Title);
		}
	}
}
