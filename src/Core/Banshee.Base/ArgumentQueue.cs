
/***************************************************************************
 *  ArgumentQueue.cs
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

namespace Banshee.Base
{
    public class ArgumentLayout
    {
        public string Name, ValueKind, Description;
        
        public ArgumentLayout(string name, string description) : this(name, 
            null, description)
        {
        }
        
        public ArgumentLayout(string name, string valueKind, string description)
        {
            Name = name;
            ValueKind = valueKind;
            Description = description;
        }
    }

    public class ArgumentQueue : IEnumerable
    {
        private ArgumentLayout [] availableArgs; 
        private Hashtable args = new Hashtable();
        private ArrayList files = new ArrayList();

        public ArgumentQueue(ArgumentLayout [] availableArgs, string [] args, string enqueueArg)
        {
            this.availableArgs = availableArgs;
        
            for(int i = 0; i < args.Length; i++) {
                string arg = null, val = String.Empty;
        
                if(args[i] == "--" + enqueueArg && i < args.Length - 1) {
                    for(int j = i + 1; j < args.Length; j++) {
                        EnqueueFile(args[j]);
                    };
                    break;
                }
        
                if(args[i].StartsWith("--")) {
                    arg = args[i].Substring(2);
                }

                if(i < args.Length - 1 && arg != null) {
                    if(!args[i + 1].StartsWith("--")) {
                        val = args[i + 1];
                        i++;
                    }
                }
                
                if(arg != null) {
                    Enqueue(arg, val);
                } else {
                    EnqueueFile(args[i]);
                }
            }
        }

        public void EnqueueFile(string file)
        {
            files.Add(file);
        }

        public void Enqueue(string arg)
        {
            Enqueue(arg, String.Empty);
        }

        public void Enqueue(string arg, string val)
        {
            args.Add(arg, val);
        }

        public string Dequeue(string arg)
        {
            string ret = args[arg] as string;
            args.Remove(arg);
            return ret;
        }

        public bool Contains(string arg)
        {
            return args[arg] != null;
        }

        public string this [string arg] {
            get {
                return args[arg] as string;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return args.GetEnumerator();
        }

        public string [] Arguments {
            get {
                ArrayList list = new ArrayList(args.Keys);
                return list.ToArray(typeof(string)) as string [];
            }
        }

        public string [] Files {
            get {
                return files.ToArray(typeof(string)) as string [];
            }
        }

        public ArgumentLayout [] AvailableArguments {
            get {
                return availableArgs;
            }
        }
    }
}
