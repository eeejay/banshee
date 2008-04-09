/*************************************************************************** 
 *  SQLiteUtility.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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
using System.Data;
using System.Text;

using Mono.Data.SqliteClient;

namespace Migo.Syndication.Data
{   
    public static class SQLiteUtility
    {
        public static string ParameterIdentifier 
        { 
            get { return ":"; } 
        }
      
        public static string LastInsertIDQuery 
        { 
            get { return "SELECT last_insert_rowid();"; } 
        }        
        
        public static IDbConnection NewConnection (string connectionString)
        {
            if (String.IsNullOrEmpty (connectionString)) {
            	throw new ArgumentException ("connectionString:  Cannont be null or empty.");
            }
            
            return (IDbConnection)(new SqliteConnection (connectionString)); 
        }           

        public static IDbConnection GetNewConnection (string uri)
        {
            return GetNewConnection (uri, 3, -1, false);
        }         
        
        public static IDbConnection GetNewConnection (string uri, int version)
        {
            return GetNewConnection (uri, version, -1, false);
        }          
        
        public static IDbConnection GetNewConnection (string uri, 
                                                                                         int version, 
                                                                                         int busyTimeout)
        {
            return GetNewConnection (uri, version, busyTimeout, false);
        }
        
        public static IDbConnection GetNewConnection (string uri, 
                                                                                        int version, 
                                                                                        int busyTimeout, 
                                                                                        bool inMemory)
        {
            return (IDbConnection)(new SqliteConnection (BuildConnectionString (
                uri, version, busyTimeout, inMemory                
            )));   
        }
                    
        private static string BuildConnectionString (string uri, 
                                                                                   int version, 
                                                                                   int busyTimeout, 
                                                                                   bool inMemory)
        {   
            if (String.IsNullOrEmpty (uri) && !inMemory) {
                throw new InvalidOperationException (
                    "Uri must be defined if database is not in memory only."
                );
            } else if (version < 2 || version > 3) {
                throw new ArgumentOutOfRangeException (
                    "Version number must be either 2 or 3"
                );	
            }
            
            StringBuilder sb = new StringBuilder ();

            if (version > 0) {
            	sb.AppendFormat ("version={0},", version);
            }            
            
            sb.AppendFormat (
                "URI=file:{0}",
                inMemory ? ":memory:" : uri                                 
            );   
                        
            if (busyTimeout > 0) {
            	sb.AppendFormat (",busy_timeout={0}", busyTimeout);
            }
            
            Console.WriteLine (sb.ToString ());
            
            return sb.ToString ();
        }
    }
}

