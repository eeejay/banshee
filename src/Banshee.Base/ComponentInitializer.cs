/***************************************************************************
 *  ComponentInitializer.cs
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
using System.Collections.Generic;

using Mono.Unix;

namespace Banshee.Base
{
    public delegate void ComponentInitializerHandler();
    public delegate void ComponentInitializingHandler(object o, ComponentInitializingArgs args);
    
    public class ComponentInitializingArgs 
    {
        private string name;
        private int current;
        private int total;
        
        public ComponentInitializingArgs(string name, int current, int total)
        {
            this.name = name;
            this.current = current;
            this.total = total;
        }
        
        public string Name {
            get { return name; }
        }
        
        public int Current {
            get { return current; }
        }
        
        public int Total {
            get { return total; }
        }
    }
    
    public class ComponentInitializer
    {
        protected class Component 
        {
            public string Name;
            public ComponentInitializerHandler Initialize;
            public bool CatchExceptions;
            public string FailureMessage;
        }

        private Queue<Component> components = new Queue<Component>();
        private int component_run_count = 0;
        
        public event ComponentInitializingHandler ComponentInitializing;
        public event EventHandler RunFinished;
        
        public ComponentInitializer()
        {
        }
        
        public void Register(string name, ComponentInitializerHandler initializer)
        {
            Register(name, false, null, initializer);
        }
        
        public void Register(string name, bool catchExceptions, ComponentInitializerHandler initializer)
        {
            Register(name, catchExceptions, Catalog.GetString("Could not initialize component"), initializer);
        }
        
        public void Register(string name, bool catchExceptions, string failureMessage,
            ComponentInitializerHandler initializer)
        {
            Component component = new Component();
            
            component.Name = name;
            component.Initialize = initializer;
            component.CatchExceptions = catchExceptions;
            component.FailureMessage = failureMessage;
            components.Enqueue(component);
            
            component_run_count++;
        }
        
        public void Run()
        {
            int current = 1;
            
            while(components.Count > 0) {
                Component component = components.Dequeue();
                if(component.CatchExceptions) {
                    try {
                        component.Initialize();
                    } catch(Exception e) {
                    
                    }
                } else {
                    component.Initialize();
                }
                
                OnInitialized(component, current++);
            }
            
            component_run_count = 0;
            OnRunFinished();
        }
        
        protected virtual void OnInitialized(Component component, int current)
        {
            ComponentInitializingHandler handler = ComponentInitializing;
            if(handler != null) {
                handler(this, new ComponentInitializingArgs(component.Name, current, component_run_count));
            }
        }
        
        protected virtual void OnRunFinished()
        {
            EventHandler handler = RunFinished;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
    }
}
