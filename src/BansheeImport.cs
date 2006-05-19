/***************************************************************************
 *  BansheeImport.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.Collections;

using Mono.Unix;

using Banshee.Base;
using Banshee.IO;
using Entagged;
using Sql;

public static class BansheeImport
{
    private static bool verbose = false;
    private static Queue import_queue = new Queue();
    private static BansheeDatabase database;
    private static int import_success = 0;
    private static int import_total = 0;

    public static int Main(string [] args)
    {
        foreach(string arg in args) {
            switch(arg) {
                case "--version":
                    PrintVersion();
                    return 1;
                case "--help":
                    PrintHelp();
                    return 1;
                case "--verbose":
                    verbose = true;
                    break;
                default:
                    import_queue.Enqueue(arg);
                    break;
            }
        }
        
        if(!Directory.Exists(Paths.ApplicationData)) {
            Directory.CreateDirectory(Paths.ApplicationData);
        }
        
        database = new BansheeDatabase(Path.Combine(Paths.ApplicationData, "banshee.db"));
        
        while(import_queue.Count > 0) {
            ProcessInput(import_queue.Dequeue() as string);
        }

        Debug("{0} / {1} files imported", import_success, import_total);

        return 1;
    }

    private static void ProcessInput(string source)
    {
        bool is_regular_file = false;
        bool is_directory = false;
      
        try {
            is_regular_file = IOProxy.File.Exists(source);
            is_directory = !is_regular_file && IOProxy.Directory.Exists(source);
        } catch {
            return;
        }
      
        if(is_regular_file && !Path.GetFileName(source).StartsWith(".")) {
            ImportFile(source);
        } else if(is_directory && !Path.GetFileName(System.IO.Path.GetDirectoryName(source)).StartsWith(".")) {
            try {
                foreach(string file in IOProxy.Directory.GetFiles(source)) {
                    ProcessInput(file);
                }

                foreach(string directory in IOProxy.Directory.GetDirectories(source)) {
                    ProcessInput(directory);
                }
            } catch(System.UnauthorizedAccessException) {
            }
        }
    }
    
    private static void ImportFile(string path)
    {
        Debug("Importing: {0}", path);
        
        import_total++;
        
        try {
            AudioFile af = new AudioFile(path);
            Statement insert = new Insert("Tracks", true,
                "TrackID", null, 
                "Uri", FilenameToUri(path),
                "MimeType", af.MimeType, 
                "Artist", af.Artist, 
                "AlbumTitle", af.Album,
                "Title", af.Title, 
                "Genre", af.Genre, 
                "Year", af.Year,
                "DateAddedStamp", DateTimeUtil.FromDateTime(DateTime.Now), 
                "TrackNumber", af.TrackNumber, 
                "TrackCount", af.TrackCount, 
                "Duration", (int)af.Duration.TotalSeconds);
            database.Execute(insert);
            import_success++;
        } catch(Exception e) {
            Debug("** Could not import: {0}", path);
            Debug(e.ToString());
        }
    }
    
    public static string FilenameToUri(string localPath)
    {
        IntPtr path_ptr = UnixMarshal.StringToHeap(localPath);
        IntPtr uri_ptr = g_filename_to_uri(path_ptr, IntPtr.Zero, IntPtr.Zero);
        UnixMarshal.FreeHeap(path_ptr);

        if(uri_ptr == IntPtr.Zero) {
            throw new ApplicationException("Filename path must be absolute");
        }

        string uri = UnixMarshal.PtrToString(uri_ptr);
        UnixMarshal.FreeHeap(uri_ptr);

        return uri;
    }
    
    [System.Runtime.InteropServices.DllImport("libglib-2.0.so")]
    private static extern IntPtr g_filename_to_uri(IntPtr filename, IntPtr hostname, IntPtr error);
    
    private static void Debug(string message, params object [] args)
    {
        if(verbose) {
            Console.WriteLine(message, args);
        }
    }
    
    private static void PrintVersion()
    {
        Console.WriteLine("Banshee " + ConfigureDefines.VERSION);
    }
    
    private static void PrintHelp()
    {
        Console.WriteLine("Usage: banshee-import [ options ... ] <import_1> [ ... <import_N> ]");
        Console.WriteLine("       where options include:\n");
        Console.WriteLine("  --verbose      print verbose messages");
        Console.WriteLine("  --help         show this help");
        Console.WriteLine("  --version      show version");
        Console.WriteLine("");
    }
}
