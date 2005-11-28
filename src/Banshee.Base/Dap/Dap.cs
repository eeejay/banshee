/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Dap.cs
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
using System.Collections;

namespace Banshee.Dap
{
    public enum DapType {
        Generic,
        NonGeneric
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DapProperties : Attribute 
    {
        private DapType dap_type;
        
        public DapType DapType {
            get {
                return dap_type;
            }
            
            set {
                dap_type = value;
            }
        }
    }
    
    public class CannotHandleDeviceException : ApplicationException
    {
        public CannotHandleDeviceException() : base("HAL Device cannot be handled by Dap subclass")
        {
        }
    }
    
    public abstract class Dap
    {
        public class PropertyTable : IEnumerable
        {
            public class Property
            {
                public string Name;
                public string Value;
            }
        
            private ArrayList properties = new ArrayList();
            
            private Property Find(string name)
            {
                foreach(Property property in properties) {
                    if(property.Name == name) {
                        return property;
                    }
                }
                
                return null;
            }
            
            public void Add(string name, string value)
            {
                if(value == null || value.Trim() == String.Empty) {
                    return;
                }
                
                Property property = Find(name);
                if(property != null) {
                    property.Value = value;
                    return;
                } 
                
                property = new Property();
                property.Name = name;
                property.Value = value;
            
                properties.Add(property);
            }
            
            public string this [string name] {
                get {
                    Property property = Find(name);
                    return property == null ? null : property.Value;
                }
            }
            
            public IEnumerator GetEnumerator()
            {
                foreach(Property property in properties) {
                    yield return property.Name;
                }
            }
        }

        private PropertyTable properties = new PropertyTable();
        
        protected void InstallProperty(string name, string value)
        {
            properties.Add(name, value);
        }
        
        public PropertyTable Properties {
            get {
                return properties;
            }
        }
        
        public abstract string Name { get; set; }
        public abstract ulong StorageCapacity { get; }
        public abstract ulong StorageUsed { get; }
        public abstract bool IsReadOnly { get; }
        public abstract bool IsPlaybackSupported { get; }
    }
}
