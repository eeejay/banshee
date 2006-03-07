
/***************************************************************************
 *  DaapProxyWebServer.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
 *             James Wilcox <snorp@snorp.net>
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
using System.IO;
using System.Text;
using System.Web;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

using DAAP;

namespace Banshee.Plugins.Daap
{
    internal class DaapProxyWebServer
    {
        private const int ChunkLength = 8192;

        private ushort port;
        private Socket server;
        private bool running;
        private ArrayList clients = new ArrayList();
        private ArrayList databases = new ArrayList();
      
        public DaapProxyWebServer() 
        {
        }

        public void Start() 
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try {
                server.Bind(new IPEndPoint(IPAddress.Any, 8089));
            } catch (System.Net.Sockets.SocketException) {
                server.Bind(new IPEndPoint(IPAddress.Any, 0));
            }
            port = (ushort)(server.LocalEndPoint as IPEndPoint).Port;
            server.Listen(10);

            running = true;
            Thread thread = new Thread(ServerLoop);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Stop() 
        {
            running = false;
            
            if(server != null) {
                server.Close();
                server = null;
            }

            foreach(Socket client in (ArrayList)clients.Clone()) {
                client.Close();
            }
        }
        
        public void RegisterDatabase(DAAP.Database database)
        {
            databases.Add(database);
        }
        
        public void UnregisterDatabase(DAAP.Database database)
        {
            databases.Remove(database);
        }
        
        private void ServerLoop()
        {
            while(true) {
                try {
                    if(!running) {
                        break;
                    }
                    
                    Socket client = server.Accept();
                    clients.Add(client);
                    ThreadPool.QueueUserWorkItem(HandleConnection, client);
                } catch(SocketException e) {
                    break;
                }
            }
        }
        
        private void HandleConnection(object o) 
        {
            Socket client = (Socket)o;

            try {
                while(HandleRequest(client));
            } catch(IOException e) {
            } catch(Exception e) {
                Console.Error.WriteLine("Error handling request: " + e);
            } finally {
                clients.Remove(client);
                client.Close();
            }
        }
        
        private bool HandleRequest(Socket client) 
        {
            if(!client.Connected) {
                return false;
            }
            
            bool keep_connection = true;
            
            using(StreamReader reader = new StreamReader(new NetworkStream(client, false))) {
                string request = reader.ReadLine();
                
                if(request == null) {
                    return false;
                }
                
                string line = null;
                
                do {
                    line = reader.ReadLine();
                    if(line.ToLower() == "connection: close") {
                        keep_connection = false;
                    }
                } while(line != String.Empty && line != null);
                
                string [] split_request = request.Split();
                
                if(split_request.Length < 3) {
                    WriteResponse(client, HttpStatusCode.BadRequest, "Bad Request");
                    return keep_connection;
                } else {
                    try {
                        HandleValidRequest(client, split_request);
                    } catch(IOException e) {
                        keep_connection = false;
                    } catch(Exception e) {
                        keep_connection = false;
                        Console.Error.WriteLine("Trouble handling request {0}: {1}", split_request[1], e);
                    }
                }
            }

            return keep_connection;
        }

        private void HandleValidRequest(Socket client, string [] split_request)
        {        
            if(split_request[1].StartsWith("/")) {
               split_request[1] = split_request[1].Substring(1);
            }

            string [] nodes = split_request[1].Split('/');
            string body = String.Empty;
            HttpStatusCode code = HttpStatusCode.OK;

            if(nodes.Length == 1 && nodes[0] == String.Empty) {
               body = GetHtmlHeader("Available Databases");
               
               if(databases.Count == 0) {
                   body += "<blockquote><p><em>No databases found. Connect to a " + 
                       "share in Banshee.</em></p></blockquote>";
               } else {
                   body += "<ul>";
                   foreach(DAAP.Database database in (ArrayList)databases.Clone()) {
                       body += String.Format("<li><a href=\"/{0}\">{1} ({2} Songs)</a></li>",
                           database.GetHashCode(), database.Name, database.SongCount);
                   }
                   body += "</ul>";
               }
            } else if(nodes.Length == 1 && nodes[0] != String.Empty) {
                bool db_found = false;
                int id = 0;
                try {
                    id = Convert.ToInt32(nodes[0]);
                } catch {
                }
                
                foreach(DAAP.Database database in (ArrayList)databases.Clone()) {
                    if(database.GetHashCode() != id) {
                        continue;
                    }
                    
                    body = GetHtmlHeader("Songs in " + database.Name);
                    
                    if(database.SongCount == 0) {
                        body += "<blockquote><p><em>No songs in this database.</em></p></blockquote>";
                    } else {
                        body += "<p>Showing all " + database.SongCount + " songs:</p><ul>";
                        foreach(DAAP.Song song in database.Songs) {
                            body += String.Format("<li><a href=\"/{0}/{1}\">{2} - {3}</a> ({4}:{5})</li>",
                                database.GetHashCode(), song.Id, song.Artist, song.Title, 
                                song.Duration.Minutes, song.Duration.Seconds.ToString("00"));
                        }
                        body += "</ul>";
                    }
                    
                    db_found = true;
                    break;
                }
                
                if(!db_found) {
                    code = HttpStatusCode.BadRequest;
                    body = GetHtmlHeader("Invalid Request");
                    body += String.Format("<p>No database with id `{0}'</p>", id);
                }
            } else if(nodes.Length == 2) {
                bool db_found = false;
                int db_id = 0;
                int song_id = 0;
                
                try {
                    db_id = Convert.ToInt32(nodes[0]);
                    song_id = Convert.ToInt32(nodes[1]);
                } catch {
                }
                
                foreach(DAAP.Database database in (ArrayList)databases.Clone()) {
                    if(database.GetHashCode() != db_id) {
                        continue;
                    }
                    
                    try {
                        Song song = database.LookupSongById(song_id);
                        if(song != null) {
                            StreamSong(client, database, song);
                            return;
                        }
                    } catch {
                    }
                        
                    code = HttpStatusCode.BadRequest;
                    body = GetHtmlHeader("Invalid Request");
                    body += String.Format("<p>No song with id `{0}'</p>", song_id);
                    
                    db_found = true;
                    break;
                }
                
                if(!db_found) {
                    code = HttpStatusCode.BadRequest;
                    body = GetHtmlHeader("Invalid Request");
                    body += String.Format("<p>No database with id `{0}'</p>", db_id);
                }
            } else {
               code = HttpStatusCode.BadRequest;
               body = GetHtmlHeader("Invalid Request");
               body += String.Format("<p>The request '{0}' could not be processed by server.</p>",
                   split_request[1]);
            }

            WriteResponse(client, code, body + GetHtmlFooter());
        }
        
        private void StreamSong(Socket client, DAAP.Database database, DAAP.Song song)
        {
            long length;
            Stream stream = database.StreamSong(song, out length);
            WriteResponseStream(client, stream, length, song.FileName);
            stream.Close();
            client.Close();
        }

        private void WriteResponse(Socket client, HttpStatusCode code, string body) 
        {
            WriteResponse(client, code, Encoding.UTF8.GetBytes(body));
        }
        
        private void WriteResponse(Socket client, HttpStatusCode code, byte [] body) 
        {
            if(!client.Connected) {
                return;
            }
            
            string headers = String.Empty;
            headers += String.Format("HTTP/1.1 {0} {1}\r\n", (int)code, code.ToString());
            headers += String.Format("Content-Length: {0}\r\n", body.Length);
            headers += "Content-Type: text/html\r\n";
            headers += "Connection: close\r\n";
            headers += "\r\n";
            
            using(BinaryWriter writer = new BinaryWriter(new NetworkStream(client, false))) {
                writer.Write(Encoding.UTF8.GetBytes(headers));
                writer.Write(body);
            }
            
            client.Close();
        }

        private void WriteResponseStream(Socket client, Stream response, long length, string filename)
        {
            using(BinaryWriter writer = new BinaryWriter(new NetworkStream(client, false))) {
                string headers = "HTTP/1.1 200 OK\r\n";

                if(length > 0) {
                    headers += String.Format("Content-Length: {0}\r\n", length);
                }
                
                if(filename != null) {
                    headers += String.Format("Content-Disposition: attachment; filename=\"{0}\"\r\n",
                        filename.Replace("\"", "\\\""));
                }
                
                headers += "Connection: close\r\n";
                headers += "\r\n";
                
                writer.Write(Encoding.UTF8.GetBytes(headers));

                using(BinaryReader reader = new BinaryReader(response)) {
                    while(true) {
                        byte [] buffer = reader.ReadBytes(ChunkLength);
                        if(buffer == null) {
                            break;
                        }
                        
                        writer.Write(buffer);
                        
                        if(buffer.Length < ChunkLength) {
                            break;
                        }
                    }
                }
            }
        }
        
        private static string GetHtmlHeader(string title)
        {
            return String.Format("<html><head><title>{0} - Banshee DAAP Browser</title></head><body><h1>{0}</h1>", 
                title);
        }
        
        private static string GetHtmlFooter()
        {
            return String.Format("<hr /><address>Generated on {0} by " + 
                "Banshee DAAP Plugin (<a href=\"http://banshee-project.org\">http://banshee-project.org</a>)",
                DateTime.Now.ToString());
        }

        public ushort Port {
            get { 
                return port;
            }
        }

        private static IPAddress local_address = IPAddress.Parse("127.0.0.1");
        public IPAddress IPAddress {
            get {
                return local_address;
            }
        }
        
        public string HttpBaseAddress {
            get {
                return String.Format("http://{0}:{1}/", IPAddress, Port);
            }
        }
    }
}
