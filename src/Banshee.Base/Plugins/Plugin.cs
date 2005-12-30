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
        private string name;
        private NameValueCollection configuration_keys;
    
        public Plugin()
        {
            string full_name = GetType().Namespace;
            string base_full_name = GetType().BaseType.Namespace;

            if(!full_name.StartsWith(base_full_name + ".")) {
                throw new InvalidPluginException(
                    String.Format("Plugin `{0}' has an invalid namespace ({1}). " + 
                        "Plugin namespace must start with {2}. For example, {2}.MyPlugin", 
                        GetType().FullName, full_name, base_full_name));
            }
            
            name = full_name.Substring(base_full_name.Length + 1);
            
            configuration_keys = new NameValueCollection();
        }
        
        protected void RegisterConfigurationKey(string name)
        {
            configuration_keys[name] = ConfigurationBase + "/" + name;
        }
        
        protected NameValueCollection ConfigurationKeys {
            get {
                return configuration_keys;
            }
        }
    
        public abstract void Initialize();
        
        public virtual void Dispose()
        {
        }
        
        public string Name {
            get {
                return name;
            }
        }
        
        protected string ConfigurationBase {
            get {
                return GConfKeys.BasePath + Name;
            }
        }
    }
}
