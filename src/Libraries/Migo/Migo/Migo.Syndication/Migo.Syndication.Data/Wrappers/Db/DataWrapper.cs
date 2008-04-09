/*************************************************************************** 
 *  DataWrapper.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
 ****************************************************************************/
 
/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted,free of charge,to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"), 
 *  to deal in the Software without restriction,including without limitation  
 *  the rights to use,copy,modify,merge,publish,distribute,sublicense, 
 *  and/or sell copies of the Software,and to permit persons to whom the  
 *  Software is furnished to do so,subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS",WITHOUT WARRANTY OF ANY KIND,EXPRESS OR 
 *  IMPLIED,INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,DAMAGES OR OTHER 
 *  LIABILITY,WHETHER IN AN ACTION OF CONTRACT,TORT OR OTHERWISE,ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Data;

namespace Migo.Syndication.Data
{    
    public class DataWrapper : IDisposable
    {
        private IDataReader reader;
        
        protected virtual IDataReader Reader
        {   
            get { 
                CheckReader ();
                return reader;
            }
        }
        
        public virtual bool IsClosed
        {
            get { 
                CheckReader (); 
                return reader.IsClosed; 
            }    
        }        
        
        public DataWrapper (IDataReader reader)
        {
            if (reader == null) {
                throw new ArgumentNullException ("reader");    	
            }
            
            this.reader = reader;
            
            CheckReader ();            
        }
        
        public virtual void Close ()
        {
            Dispose ();
        }
        
        public virtual void Dispose ()
        {
            CheckReader ();
            
            if (reader != null) {
                reader.Dispose ();
                reader = null;            	
            }
        }

        public virtual bool GetBooleanSafe (int index)
        {
            CheckReader ();
            return (Reader.IsDBNull (index)) ? 
                false : Reader.GetBoolean (index);
        }

        public virtual DateTime GetDateTimeSafe (int index) 
        {
            CheckReader ();        
            return (Reader.IsDBNull (index)) ? 
                DateTime.MinValue : Reader.GetDateTime (index).ToLocalTime ();
        }

        public virtual double GetDoubleSafe (int index) 
        {
            CheckReader ();        
            return (Reader.IsDBNull (index)) ? 
                0 : Reader.GetDouble (index);
        }

        public virtual float GetFloatSafe (int index) 
        {
            CheckReader ();        
            return (Reader.IsDBNull (index)) ? 
                0 : Reader.GetFloat (index);
        }

        public virtual Guid GetGuidSafe (int index) 
        {
            CheckReader ();        
            return (Reader.IsDBNull (index)) ? 
                Guid.Empty : Reader.GetGuid (index);
        }        

        public virtual short GetInt16Safe (int index)
        {
            CheckReader ();        
            return (Reader.IsDBNull (index)) ? 
                (short) 0 : Reader.GetInt16 (index);
        }        
        
        public virtual int GetInt32Safe (int index)
        {
            CheckReader ();        
            return (Reader.IsDBNull (index)) ? 
                0 : Reader.GetInt32 (index);
        }
        
        public virtual long GetInt64Safe (int index)
        {
            CheckReader ();        
            return (Reader.IsDBNull (index)) ? 
                0 : Reader.GetInt64 (index);
        }         
        
        public virtual string GetStringSafe (int index) 
        {
            CheckReader ();        
            return (Reader.IsDBNull (index)) ? 
                String.Empty : Reader.GetString (index);
        }

        public virtual bool NextResult ()
        {
            CheckReader ();
            return reader.NextResult ();    
        }
                    
        public virtual bool Read ()
        {
            CheckReader ();
            return reader.Read ();    
        }      
        
        protected virtual void CheckReader ()
        {
            if (reader.IsClosed) {
            	throw new ObjectDisposedException (GetType ().FullName);
            }            
        }  
    }
}
