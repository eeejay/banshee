/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Utilities.cs
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
using System.Reflection;
using System.Text.RegularExpressions; 

namespace Banshee
{
	public class Error
	{
		private string message;
		private Exception e;	

		public Error(string message) : this(message, null)
		{
		
		}

		public Error(string message, Exception e)
		{
			this.message = message;
			this.e = e;
		
			//if(e != null)	
				//Console.WriteLine("Error: {0} ({1})", message, e.Message);
			//else
				//Console.WriteLine("Error: {0}", message);	
		}
	}
	
	public class SonanceException : System.Exception
	{
		public SonanceException(string message) 
		{
			new Error(message);
		}
	}
	
	public class StringUtil
	{
		public static string EntityEscape(string str)
		{
			if(str == null)
				return null;
				
			return str.Replace("&", "&amp;");
		}
	
		private static string RegexHexConvert(Match match)
		{
			int digit = Convert.ToInt32(match.Groups[1].ToString(), 16);
			return Convert.ToChar(digit).ToString();
		}	
				
		public static string UriEscape(string uri)
		{
			return Regex.Replace(uri, "%([0-9A-Fa-f][0-9A-Fa-f])", 
				new MatchEvaluator(RegexHexConvert));
		}
		
		public static string UriToFileName(string uri)
		{
			uri = UriEscape(uri).Trim();
			if(!uri.StartsWith("file://"))
				return uri;
				
			return uri.Substring(7);
		}
		
		public static string UcFirst(string str)
		{
			return Convert.ToString(str[0]).ToUpper() + str.Substring(1);
		}
	}
	
	public class Paths
	{
		public static string ApplicationData
		{
			get {
				return Environment.GetFolderPath(
					Environment.SpecialFolder.ApplicationData) 
					+ Path.DirectorySeparatorChar 
					+ "banshee" 
					+ Path.DirectorySeparatorChar;
			}
		}
		
		public static string DefaultLibraryPath
		{
			get {
				return Paths.ApplicationData + "Library";
			}
		}
		
		public static string TempDir 
		{
			get {
				string dir = Paths.ApplicationData 
					+ Path.DirectorySeparatorChar 
					+ "temp";
		
				if(File.Exists(dir))
					File.Delete(dir);

				Directory.CreateDirectory(dir);
				return dir;
			}
		}
	}
	
	public class Resource
	{
		public static string GetFileContents(string name)
		{
			Assembly asm = Assembly.GetExecutingAssembly();
			Stream stream = asm.GetManifestResourceStream(name);
			StreamReader reader = new StreamReader(stream);
			return reader.ReadToEnd();	
		}
	}	
}
