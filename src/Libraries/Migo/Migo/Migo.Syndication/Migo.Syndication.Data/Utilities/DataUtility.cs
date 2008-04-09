/*************************************************************************** 
 *  DataUtility.cs
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

namespace Migo.Syndication.Data
{   
    public static class DataUtility
    {        
        public static IDbCommand CreateCommand (IDbConnection conn)
        {
            IDbCommand comm = null;
            
            CheckConnection (conn);
            comm = conn.CreateCommand ();
            return comm;
        }        
        
        public static IDbCommand CreateCommand (IDbConnection conn, 
                                                string queryText)
        {
            IDbCommand comm = null;
            CheckQueryText (queryText);
            
            comm = conn.CreateCommand ();
            comm.CommandText = queryText;
            
            return comm;
        }
        
        public static IDbCommand CreateCommand (IDbConnection conn, 
                                                string queryText, 
                                                string name, 
                                                object val)
        {
            IDbCommand comm = null;
            CheckNameValuePair (name, val);
            comm = CreateCommand (conn, queryText);
            
            try {
                AddParameterInternal (comm, name, val.ToString ());
                comm.Prepare ();
            } catch {
                if (comm != null) {
                    comm.Dispose ();
                    comm = null;
                }
                
                throw;            	                
            }
                        
            return comm;
        }
        
        public static IDbCommand CreateCommand (IDbConnection conn, 
                                                string queryText, 
                                                params string[] parameters)
        {
            IDbCommand comm = null;           
            CheckParameters (parameters);                    
            comm = CreateCommand (conn, queryText);
            
            try {
                AddParametersInternal (comm, parameters);
                comm.Prepare ();
            } catch {
                if (comm != null) {
                    comm.Dispose ();
                    comm = null;
                }
                
                throw;            	                
            }
            
            return comm;
        }        

        public static string MultipleOnIDQuery (string baseQuery, 
                                                string idParameter, 
                                                long[] ids)
        {
            if (String.IsNullOrEmpty (baseQuery)) {
                throw new ArgumentException ("baseQuery:  Must not be null or empty");
            } else if (String.IsNullOrEmpty (idParameter)) {
                throw new ArgumentException ("idParameter:  Must not be null or empty");            
            } else if (ids == null) {
                throw new ArgumentNullException ("ids");
            } 
            
            StringBuilder sb = new StringBuilder (baseQuery);            
            
            sb.AppendFormat (" {0} IN (", idParameter);            
            
            int len = ids.Length;            
            
            foreach (long l in ids) {
                sb.Append (l);
                
                if (--len != 0) {
                    sb.Append (", ");                    
                }
            }
            
            sb.Append (");");
            return sb.ToString ();
        }        
        
        public static void AddParameter (IDbCommand comm, string name, string val)
        {
            CheckCommand (comm);
            CheckNameValuePair (name, val);
            
            AddParameterInternal (comm, name, val);
        }
        
        public static void AddParameterInternal (IDbCommand comm, string name, string val)
        {
            comm.Parameters.Add (
                CreateParameter (comm, name, val)
            );             
        }
        
        public static void AddParameters (IDbCommand comm, params string[] parameters)
        {               
            CheckCommand (comm);
            CheckParameters (parameters);
            
            AddParametersInternal (comm, parameters);
        }
        
        private static void AddParametersInternal (IDbCommand comm, params string[] parameters) 
        {
            int i = 0;            
            
            while (i < parameters.Length-1) {
                if (parameters[i] == null) {
                    throw new ArgumentNullException (String.Format (
                        "Parameter identifier {0}", i
                    ));
                }

                comm.Parameters.Add (CreateParameter (
                    comm, 
                    parameters[i],
                    parameters[i+1]
                ));                
                
                i += 2;
            }            
        }
        
        private static IDataParameter CreateParameter (IDbCommand comm, string name, string val)
        {
            IDataParameter ret;
            
            ret = comm.CreateParameter ();
            
            ret.Value = val;            
            ret.ParameterName = name;
            
            return ret;
        }
        
        private static void CheckCommand (IDbCommand comm)
        {
            if (comm == null) {
            	throw new ArgumentNullException ("comm");
            }
        }
        
        private static void CheckConnection (IDbConnection conn)
        {
            if (conn == null) {
            	throw new ArgumentNullException ("conn");            	
            }
        }
            
        private static void CheckNameValuePair (string name, object val)
        {
            if (String.IsNullOrEmpty (name)) {
            	throw new ArgumentException ("name:  Must not be null or empty");            	
            } else if (val == null) {
            	throw new ArgumentNullException ("val");
            }            
        }
        
        private static void CheckParameters (string[] parameters)
        {
            if (parameters == null) {
             	throw new ArgumentNullException ("parameters");
             } else if ((parameters.Length % 2) == 1) {
            	throw new ArgumentException ("parameters:  Must be even, parameters mismatched.");
            }
        }
        
        private static void CheckQueryText (string queryText)
        {
            if (String.IsNullOrEmpty (queryText)) {
            	throw new ArgumentException ("queryText:  Must not be null or empty");            	
            }            
        }
    }
}
