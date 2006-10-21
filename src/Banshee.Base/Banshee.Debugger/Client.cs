/***************************************************************************
 *  Client.cs
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
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Banshee.Debugger
{
    public class Client
    {
        private IDebuggerServer server;
        
        public void Run()
        {
            if(FindServer()) {
                Console.WriteLine("Banshee Debugger");
                Console.WriteLine("Enter remote methods by either the full type+method name");
                Console.WriteLine("or by their remote method key name. Enter 'show-methods'");
                Console.WriteLine("to get a list of available methods. 'quit' exits.\n");
                
                MainLoop();
                
                Console.WriteLine("Goodbye");
                return;
            }
            
            Console.WriteLine("ERROR: Banshee.Debugger.IDebuggerServer not found");
            return;
        }
        
        private bool FindServer()
        {
            if(!Bus.Session.NameHasOwner(Banshee.Base.DBusRemote.BusName)) {
                return false;
            }

            try {
                server = Bus.Session.GetObject<IDebuggerServer>(Banshee.Base.DBusRemote.BusName,
                    new ObjectPath(Banshee.Base.DBusRemote.ObjectRoot + "/Debugger"));
                
                if(server == null) {
                    throw new ApplicationException("Server not found");
                }
                
                server.Shutdown += delegate {
                    Environment.Exit(0);
                };
                
                return true;
            } catch {
                return false;
            }
        }
        
        private void MainLoop()
        {
            while(true) {
                string command = Prompt();
                switch(command) {
                    case "exit":
                    case "quit":
                        return;
                    default:
                        if(!RunRemoteCommand(command)) {
                            return;
                        }
                        break;
                }
            }
        }
        
        private string Prompt()
        {
            Console.Write("banshee> ");
            return Console.ReadLine();
        }
        
        private bool RunRemoteCommand(string command)
        {
            if(command == null || command == String.Empty) {
                return true;
            }
            
            try {
                string result = server.MethodCall(command);
            
                if(result == String.Empty) {
                    UnknownCommand(command);
                    return true;
                }
                
                Console.WriteLine(result);
            } catch(Exception e) {
                Console.WriteLine("Server Error: {0}", e.Message);
                try {
                    if(!FindServer()) {
                        throw new ApplicationException();
                    }
                } catch {
                    Console.WriteLine("Could not reconnect to server");
                    return false;
                }
                
                return true;
            }
            
            return true;
        }
        
        private void UnknownCommand(string command)
        {
            Console.WriteLine("Unknown command: {0}", command);
        }
    }
}
