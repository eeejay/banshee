/***************************************************************************
 *  CleanRoomStartup.cs
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

namespace Banshee.Gui
{
    public static class CleanRoomStartup
    {
        public delegate void StartupInvocationHandler(string [] args);
        
        public static void Startup(StartupInvocationHandler startup, string [] args)
        {
            bool disable_clean_room = false;
            
            foreach(string arg in args) {
                if(arg == "--disable-clean-room") {
                    disable_clean_room = true;
                    break;
                }
            }
            
            if(disable_clean_room) {
                startup(args);
                return;
            }
            
            try {
                startup(args);
            } catch(Exception e) {
                Gtk.Application.Init();
                Banshee.Gui.Dialogs.ExceptionDialog dialog = new Banshee.Gui.Dialogs.ExceptionDialog(e);
                dialog.Run();
                dialog.Destroy();
                System.Environment.Exit(1);
            }
        }
    }
}
