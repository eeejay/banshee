/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Plugin.cs
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
using System.Collections.Specialized;
using Banshee.Base;
 
namespace Banshee.Plugins
{
    public class InvalidPluginException : ApplicationException
    {
        public InvalidPluginException(string message) : base(message)
        {
        }
    }

    public abstract class Plugin
    {
        private bool initialized;
        private bool broken;
        private NameValueCollection configuration_keys;
        private bool dispose_requested;
    
        protected abstract string ConfigurationName {
            get;
        }
    
        public Plugin()
        {
            configuration_keys = new NameValueCollection();
            broken = false;
        }
        
        protected void RegisterConfigurationKey(string name)
        {
            configuration_keys[name] = ConfigurationBase + "/" + name;
        }
        
        public NameValueCollection ConfigurationKeys {
            get {
                return configuration_keys;
            }
        }
        
        internal void Initialize()
        {
            if(broken) {
                return;
            }
            
            if(initialized) {
                Dispose();
            }
            
            try {
                dispose_requested = false;
                PluginInitialize();
                initialized = true;
            } catch(Exception e) {
                LogCore.Instance.PushWarning(String.Format("Could not initialize plugin `{0}'", Name),
                    e.Message, false);
                broken = true;
                initialized = false;
            }
        }
        
        internal void Dispose()
        {
            if(initialized && !broken) {
                dispose_requested = true;
                PluginDispose();
                configuration_keys.Clear();
                initialized = false;
            }
        }
        
        protected abstract void PluginInitialize();
        
        protected virtual void PluginDispose()
        {
        }

        public virtual Gtk.Widget GetConfigurationWidget()
        {
            return null;
        }
        
        protected bool DisposeRequested {
            get {
                return dispose_requested;
            }
        }
        
        internal bool HasConfigurationWidget {
            get {
                return ReflectionUtil.IsVirtualMethodImplemented(GetType(), "GetConfigurationWidget");
            }
        }
        
        internal bool Broken {
            get {
                return broken;
            }
        }
        
        public string Name {
            get {
                return ConfigurationName;
            }
        }
        
        public bool Initialized {
            get {
                return initialized;
            }
        }
        
        public string ConfigurationBase {
            get {
                return GConfKeys.BasePath + Name;
            }
        }
        
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public abstract string [] Authors { get; }
    }
}
