/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Main.cs
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
using System.Diagnostics;
using Gnome;
using Mono.Unix;

using Banshee.Base;

namespace Banshee
{    
    public static class BansheeEntry
    {    
        public static void Main(string [] args)
        {
#if EXCEPTION_DIALOG        
            try {
                Startup(args);
            } catch(Exception e) {
                Console.Error.WriteLine(e);
                Gtk.Application.Init();
                ExceptionDialog dlg = new ExceptionDialog(e);
                dlg.Run();
                dlg.Destroy();
                System.Environment.Exit(1);
            }
#else
            Startup(args);
#endif
        }
        
        private static void Startup(string [] args)
        {
            Core.ArgumentQueue = new ArgumentQueue(new ArgumentLayout [] {
                new ArgumentLayout("show",           "Show window"),
                new ArgumentLayout("hide",           "Hide window"),
                new ArgumentLayout("next",           "Play next song"),
                new ArgumentLayout("previous",       "Play previous song"),
                new ArgumentLayout("toggle-playing", "Toggle playing of current song"),
                new ArgumentLayout("play",           "Play current song"),
                new ArgumentLayout("pause",          "Pause current song"),
                new ArgumentLayout("query-artist",   "Get artist name for current playing song"),
                new ArgumentLayout("query-album",    "Get album name for current playing song"),
                new ArgumentLayout("query-title",    "Get track title for current playing song"),
                new ArgumentLayout("query-genre",    "Get track genre for current playing song"),
                new ArgumentLayout("query-duration", "Get duration of current playing song in seconds"),
                new ArgumentLayout("query-position", "Get position of current playing song in seconds"),
                new ArgumentLayout("query-uri",      "Get URI of current playing song"),
                new ArgumentLayout("query-status",   "Get player status (-1: Not loaded, 0: Paused, 1: Playing)"),
                new ArgumentLayout("hide-field",     "Do not display field name for --query-* results"),
                new ArgumentLayout("help",           "List available command line arguments"),
                new ArgumentLayout("audio-cd <dev>", "Start Banshee and/or select source mapped to <device>"),
                new ArgumentLayout("dap <dev>",      "Start Banshee and/or select source mapped to <device>"),
                new ArgumentLayout("version",        "Show Banshee Version")
            }, args);

            HandleShallowCommands();
            BansheeCore dbus_core = DetectInstanceAndDbus();
            HandleDbusCommands(dbus_core);

            Core.Args = args;
            Core.Instance.Program = new Program(StringUtil.UcFirst(ConfigureDefines.PACKAGE), 
                ConfigureDefines.VERSION, Modules.UI, args);
            
            new Banshee.PlayerUI();
            Gtk.Application.Run();
        }
    
        private static BansheeCore DetectInstanceAndDbus()
        {
            try {
                return BansheeCore.FindInstance();
            } catch(Exception) {
                Process current_process = Process.GetCurrentProcess();
                foreach(Process process in Process.GetProcesses()) {
                    if(process.ProcessName == current_process.ProcessName && process.Id != current_process.Id) {
                        Console.WriteLine(Catalog.GetString(""));
                        Console.WriteLine(Catalog.GetString("Banshee is already running. If you were trying " + 
                            "to control the already running instance of Banshee, D-Bus must be enabled. " +
                            "Banshee could not connect to your D-Bus Session Bus."));
                        System.Environment.Exit(1);
                    }
                }

                return null;
            }
        }
        
        private static void HandleDbusCommands(BansheeCore dbusCore)
        {
            if(dbusCore == null) {
                return;
            }
            
            bool present = true;
        
            foreach(string arg in Core.ArgumentQueue.Arguments) {
                bool dequeue = true;
                switch(arg) {
                case "toggle-playing":
                    dbusCore.TogglePlaying();
                    present = false;
                    break;
                case "play":
                    dbusCore.Play();
                    present = false;
                    break;
                case "pause":
                    dbusCore.Pause();
                    present = false;
                    break;
                case "show":
                    dbusCore.ShowWindow();
                    present = false;
                    break;
                case "hide":
                    dbusCore.HideWindow();
                    present = false;
                    break;
                case "next":
                    dbusCore.Next();
                    present = false;
                    break;
                case "previous":
                    dbusCore.Previous();
                    present = false;
                    break;
                case "query-artist":
                    PrintQueryResult("Artist", dbusCore.GetPlayingArtist());
                    present = false;
                    break;
                case "query-album":
                    PrintQueryResult("Album", dbusCore.GetPlayingAlbum());
                    present = false;
                    break;
                case "query-title":
                    PrintQueryResult("Title", dbusCore.GetPlayingTitle());
                    present = false;
                    break;
                case "query-genre":
                    PrintQueryResult("Genre", dbusCore.GetPlayingGenre());
                    present = false;
                    break;
                case "query-duration":
                    PrintQueryResult("Duration", dbusCore.GetPlayingDuration());
                    present = false;
                    break;
                case "query-position":
                    PrintQueryResult("Position", dbusCore.GetPlayingPosition());
                    present = false;
                    break;
                case "query-uri":
                    PrintQueryResult("Uri", dbusCore.GetPlayingUri());
                    present = false;
                    break;
                case "query-status":
                    PrintQueryResult("Status", dbusCore.GetPlayingStatus());
                    present = false;
                    break;
                case "set-position":
                    string string_position = Core.ArgumentQueue[arg];
                    int position = 0;
                    try {
                        position = Convert.ToInt32(string_position);
                        dbusCore.SetPlayingPosition(position);
                    } catch(Exception) {
                        Console.WriteLine("Invalid position `{0}'. Integer value expected.", string_position);
                    }
                    present = false;
                    break;
                case "audio-cd":
                    dbusCore.SelectAudioCd(Core.ArgumentQueue[arg]);
                    dequeue = false;
                    Present(dbusCore);
                    break;
                case "dap":
                    dbusCore.SelectDap(Core.ArgumentQueue[arg]);
                    dequeue = false;
                    Present(dbusCore);
                    break;
                case "hide-field":
                    dequeue = false;
                    break;
                }
                
                if(dequeue) {
                    Core.ArgumentQueue.Dequeue(arg);
                }
            }
            
            if(present) {
                Present(dbusCore);
            }
            
            // Major nasty hack to work around dbus-sharp bug: bad IL in object Finalizer
            System.GC.SuppressFinalize(dbusCore);
            System.Environment.Exit(0);
        }
        
        private static void PrintQueryResult(string field, object value)
        {
            if(Core.ArgumentQueue.Contains("hide-field")) {
                Console.WriteLine(value);
            } else {
                Console.WriteLine("{0}: {1}", field, value);
            }
        }
        
        private static void Present(BansheeCore dbusCore)
        {
            dbusCore.PresentWindow();
        }
        
        private static void HandleShallowCommands()
        {
            if(Core.ArgumentQueue.Contains("print-todo")) {
                BansheeTodo.PrintReport();
                Console.WriteLine("");
                System.Environment.Exit(0);
            }
            
            if(Core.ArgumentQueue.Contains("version")) {
                Console.WriteLine("Banshee " + ConfigureDefines.VERSION);
                System.Environment.Exit(0);
            }
            
            if(!Core.ArgumentQueue.Contains("help")) {
                return;
            }
            
            int max_name_len = 0;
            int max_var_len = 0;

            foreach(ArgumentLayout layout in Core.ArgumentQueue.AvailableArguments) {
                if(layout.Name.Length > max_name_len) {
                    max_name_len = layout.Name.Length;
                }

                if(layout.ValueKind != null && layout.ValueKind.Length > max_var_len) {
                    max_var_len = layout.ValueKind.Length;
                }
            }

            max_var_len = max_var_len > 0 ? max_var_len + 3 : 0;
            max_name_len++;

            Console.WriteLine("Usage: banshee [ options ... ]\n       where options include:\n");

            foreach(ArgumentLayout layout in Core.ArgumentQueue.AvailableArguments) {
                Console.WriteLine("  --{0,-" + max_name_len + "} {1,-" + max_var_len + "} {2}", 
                    layout.Name, layout.ValueKind == null 
                        ? String.Empty 
                        : "<" + layout.ValueKind + ">", layout.Description);
            }

            Console.WriteLine("");
            
            System.Environment.Exit(0);
        }
    }
}

