using System;
using System.IO;
using Entagged;

public class EntaggedTest
{
	public static void Main(string [] args)
	{
		foreach(string file in Directory.GetFiles("/home/aaron/Desktop")) {
			try {
				AudioFileWrapper af = new AudioFileWrapper(file);
				Console.WriteLine(af.Year);
			} catch(Exception) {}
		}
	}
}
