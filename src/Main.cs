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
using Gnome;

namespace Banshee
{	
    public class BansheeEntry
    {	
		public static void Main(string[] args)
        {
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
        }
		
		private static void Startup(string [] args)
		{
            BansheeCore dbusCore = null;

			Core.ArgumentQueue = new ArgumentQueue(new ArgumentLayout [] {
				new ArgumentLayout("next",       "Play next song"),
				new ArgumentLayout("previous",   "Play previous song"),
				new ArgumentLayout("toggle-playing", "Toggle playing of current song"),
				new ArgumentLayout("play",       "Play current song"),
				new ArgumentLayout("pause",      "Pause current song"),
				new ArgumentLayout("help",       "List available command line arguments"),
				new ArgumentLayout("audio-cd", "device", "Start Banshee and select source mapped to <device>")
			}, args);

			if(Core.ArgumentQueue.Contains("help")) {
				int maxNameLen = 0;
				int maxVarLen = 0;

				foreach(ArgumentLayout layout in Core.ArgumentQueue.AvailableArguments) {
					if(layout.Name.Length > maxNameLen) {
						maxNameLen = layout.Name.Length;
					}

					if(layout.ValueKind != null && layout.ValueKind.Length > maxVarLen) {
						maxVarLen = layout.ValueKind.Length;
					}
				}

				maxVarLen = maxVarLen > 0 ? maxVarLen + 3 : 0;
				maxNameLen++;

				Console.WriteLine("Usage: banshee [ options ... ]\n       where options include:");

				Console.WriteLine("");
				
				foreach(ArgumentLayout layout in Core.ArgumentQueue.AvailableArguments) {
					Console.WriteLine("  --{0,-" + maxNameLen + "} {1,-" + maxVarLen + "} {2}", layout.Name, layout.ValueKind == null ? String.Empty : "<" + layout.ValueKind + ">", layout.Description);
				}

				Console.WriteLine("");

				return;
			}

            try {
                dbusCore = BansheeCore.FindInstance();
            } catch { }

            if(dbusCore != null) {
                bool present = true;

            if(args.Length > 0) {
				foreach(string arg in Core.ArgumentQueue.Arguments) {
	                switch(arg) {
    	                case "toggle-playing":
        	                dbusCore.TogglePlaying();
							Core.ArgumentQueue.Dequeue(arg);
                	        present = false;
                    		break;
                    	case "next":
                        	dbusCore.Next();
							Core.ArgumentQueue.Dequeue(arg);
                        	present = false;
                    		break;
                    	case "previous":
                        	dbusCore.Previous();
							Core.ArgumentQueue.Dequeue(arg);
	                        present = false;
    		                break;
						case "play-cd":
							dbusCore.SelectAudioCd(Core.ArgumentQueue[arg]);
							//Core.ArgumentQueue.Dequeue(arg);
							present = false;
							break;
						case "ipod":
							Console.WriteLine("IPOD: " + Core.ArgumentQueue[arg]);
							break;
						case "burn-cd":
							Console.WriteLine("BURN: " + Core.ArgumentQueue[arg]);
							break;
            	    }
				}
            }

            if(present)
                dbusCore.PresentWindow();
                return;
            }	

            System.Reflection.AssemblyName asm = 
            System.Reflection.Assembly.GetEntryAssembly().GetName();		
            string appname = StringUtil.UcFirst(asm.Name);
            string version = String.Format("{0}.{1}.{2}", asm.Version.Major, 
                asm.Version.Minor, asm.Version.Build);
                
            Core.Args = args;
            Core.Instance.Program = new Program(appname, version, Modules.UI, args);
            new Banshee.PlayerUI();
        }
    }
}

