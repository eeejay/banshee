/***************************************************************************
 *  ActiveUserEventsManager.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
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
using System.Collections.Generic;
using Gtk;
 
namespace Banshee.Widgets
{    
    public class ActiveUserEventsManager : VBox
    {
        private static ActiveUserEventsManager instance;
        public static ActiveUserEventsManager Instance {
            get { return instance; }
        }
        
        private static List<ActiveUserEvent> user_events = new List<ActiveUserEvent>();
        
        public static void Register(ActiveUserEvent userEvent)
        {
            lock(user_events) {
                if(userEvent == null) {
                    return;
                }
                
                user_events.Add(userEvent);
                RegisterUI(userEvent);
            }
        }
        
        private static void RegisterUI(ActiveUserEvent userEvent)
        {
            if(Instance != null) {
                userEvent.Disposed += OnUserEventDisposed;

                Gtk.Application.Invoke(delegate {
                    if(userEvent.Widget != null) {
                        Instance.PackStart(userEvent.Widget, false, false, 0);
                        Instance.Show();
                    }
                });
            }
        }
        
        public ActiveUserEventsManager() : base()
        {
            if(instance != null) {
                throw new ApplicationException("Only one instance is allowed");
            }
            
            Spacing = 8;
            instance = this;
            
            Hide();
            
            lock(user_events) {
                foreach(ActiveUserEvent user_event in user_events) {
                    RegisterUI(user_event);
                }
            }
        }
        
        public void Clear()
        {
            while(user_events.Count > 0) {
                (user_events[0]).Dispose();
            }
        }
        
        private bool canceling = false;
        public void CancelAll()
        {
            if(canceling) {
                return;
            }
            
            canceling = true;
            
            while(user_events.Count > 0) {
                (user_events[0]).Cancel();
            }
        }
        
        private static void OnUserEventDisposed(object o, EventArgs args)
        {
            lock(user_events) {
                user_events.Remove(o as ActiveUserEvent);
            
                Gtk.Application.Invoke(delegate {
                    Instance.Remove((o as ActiveUserEvent).Widget);
                    
                    if(user_events.Count == 0) {
                        Instance.Hide();
                    }
                });
            }
        }
    }
}
