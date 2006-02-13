
/***************************************************************************
 *  DatabaseProxy.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
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
using DAAP;

namespace Banshee.Plugins.Daap
{
    internal class DatabaseProxy : IEnumerable
    {
        private DAAP.Database database;
        
        public DatabaseProxy()
        {
        }
        
        public IEnumerator GetEnumerator()
        {
            return new DatabaseProxyEnumerator(database);
        }
        
        public DAAP.Database Database {
            set {
                database = value;
            }
            
            get {
                return database;
            }
        }
        
        private class DatabaseProxyEnumerator : IEnumerator
        {
            private DAAP.Database database;
            private int index = -1;
            
            public DatabaseProxyEnumerator(DAAP.Database database)
            {
                this.database = database;
            }

            public bool MoveNext()
            {
                if(database != null && ++index < database.SongCount) {
                    return true;
                }
                
                Reset();
                return false;
            }
            
            public void Reset()
            {
                index = -1;
            }
            
            public object Current {
                get {
                    if(database != null) {
                        DAAP.Song song = database.SongAt(index);
                        if(song != null) {
                            return new DaapTrackInfo(song, database);
                        }
                    }
                    
                    return null;
                }
            }
        }
    }
}
