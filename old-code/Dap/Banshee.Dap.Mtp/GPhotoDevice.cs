/***************************************************************************
 *  GPhotoDevice.cs
 *
 *  Copyright (C) 2006 Novell and Patrick van Staveren
 *  Written by Patrick van Staveren (trick@vanstaveren.us)
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
using System.Collections;
using System.Text;
using LibGPhoto2;

namespace Banshee.Dap.Mtp
{

public class GPhotoDevice
{
    private Context context;
    private Camera camera;
    private CameraFilesystem fs;
    private CameraList camera_list;
    private PortInfoList port_info_list;
    private CameraAbilitiesList abilities_list;

    private ArrayList files;
    public string Store;
    
    public GPhotoDevice()
    {
        context = new Context();

        port_info_list = new PortInfoList();
        port_info_list.Load();
            
        abilities_list = new CameraAbilitiesList();
        abilities_list.Load (context);
        
        camera_list = new CameraList();
    }
        
    public int Detect()
    {
        abilities_list.Detect (port_info_list, camera_list, context);
        return CameraCount;
    }
    
    public int CameraCount {
        get {
            return camera_list.Count();
        }
    }
    
    public CameraList CameraList {
        get {
            return camera_list;
        }
    }
    
    public CameraAbilitiesList AbilitiesList {
       get {
           return abilities_list;
       }
    }
        
    public void SelectCamera(int index)
    {
        camera = new Camera();

        camera.SetAbilities(abilities_list.GetAbilities(
            abilities_list.LookupModel(camera_list.GetName (index))));

        camera.SetPortInfo(port_info_list.GetInfo(
            port_info_list.LookupPath(camera_list.GetValue(index))));

        Store = "/store_00010001/"; //FIXME: autodetect this
    }
    
    public void InitializeCamera()
    {
        if (camera == null) 
            throw new InvalidOperationException();

        try {
            camera.Init (context);
        } catch (LibGPhoto2.GPhotoException e) {
            Console.WriteLine("Init() Exception: {0}", e.ToString());
        }
        fs = camera.GetFS();
        
        files = new ArrayList();
        GetFileList();
    }

    public int DiskFree {
        get {
            //FIXME: find DiskFree
            return 1;
        }
    }
    
    public int DiskTotal {
        get {
            //FIXME: find DiskTotal
            return 1;
        }
    }
    
    public string About {
        get {
            return camera.GetAbout(context).Text;
        }
    }
    
    private void GetFileList()
    {
        GetFileList(Store);
    }
        
    private void GetFileList(string dir)
    {
        if (fs == null) 
            throw new InvalidOperationException ("fs is null");
        
        //files
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
        CameraList filelist = fs.ListFiles(dir, context);
        Console.Write("Reading metadata for " + dir + "...");
        for (int i = 0; i < filelist.Count(); i++) {
            Console.Write("{0}.", i);
            string metadata;
            
            try {
                CameraFile meta = fs.GetFile(dir, filelist.GetName(i), CameraFileType.MetaData, context);
                metadata = encoding.GetString(meta.GetDataAndSize());
            } catch {
                metadata = "<Name>No Metadata Available</Name>";
            }
            if (filelist.GetName(i).IndexOf(".mp3") > 0 ||
                    filelist.GetName(i).IndexOf(".wma") > 0 ||
                    filelist.GetName(i).IndexOf(".asx") > 0 ||
                    filelist.GetName(i).IndexOf(".wav") > 0) {
                files.Add(new GPhotoDeviceFile(
                        dir, filelist.GetName(i), metadata, this));
            }
        }
        Console.Write("\n");
    
        //subdirectories
        CameraList folderlist = fs.ListFolders(dir, context);
        for (int i = 0; i < folderlist.Count(); i++) {
            GetFileList(dir + folderlist.GetName(i) + "/");
        }
    }
    
    public ArrayList FileList
    {
        get {
            return files;
        }
    }
    
    public void PutFile(GPhotoDeviceFile file)
    {
        if (fs == null || file == null)
            return;

        file.GenerateProperPath();

        string[] path_split = file.Directory.Split('/'); // split up the path
        string path_base = Store.Remove(Store.Length - 1, 1); // take store, minus the trailing slash
        string path_build = path_base; // the path_build variable is basically the "working directory" (aka cwd) here as we search for and if necessary create directories.

        for (int i = 2; i < path_split.GetLength(0); i++) {
            if (path_split[i].Trim() == "") {
                Console.WriteLine("Blank parameter found at {0}, skipping...", i);
                continue;
            }
            
            Console.Write("Checking for {0} in {1}...", path_split[i], path_build);
            
            CameraList folders = fs.ListFolders(path_build, context);
            
            bool found = false;
            for (int j = 0; j < folders.Count(); j++) // loop thru folders that exist in path_build (aka cwd)
                if (folders.GetName(j) == path_split[i]) // check to see if the folder at current index j is what we need
                    found = true; // if so, set found to true 
            
            if (!found) { // then the directory was not found.  create it!
                Console.WriteLine("not found; creating {0} in {1}...", path_split[i], path_build);
                fs.MakeDirectory(path_build, path_split[i], context);
            } else {
                Console.WriteLine("OK");
            }
            
            path_build += "/" + path_split[i]; // tack on the directory to the path_build string so next iteration we'll be looking in it
        }

        fs.PutFile(file.Directory, file.CameraFile, context);

        PutMetadata(file);

        // adds the file to the local array of files.  should this array be depreciated?
        files.Add(file);
        
        // dispose the file - free up my memory!
        file.DisposeCameraFile ();
    }
    
    public void PutMetadata(GPhotoDeviceFile file)
    {
        try {
            CameraFile meta = new CameraFile();
            meta.SetName(file.Filename);
            UTF8Encoding encoding = new UTF8Encoding();
            meta.SetDataAndSize(encoding.GetBytes(file.Metadata));
            
            meta.SetFileType(CameraFileType.MetaData);
            fs.PutFile(file.Directory, meta, context);
            meta.Dispose();
        } catch (Exception e){
            Console.WriteLine("Failed send track metadata.  Are you using the right version of the C# bindings and libgphoto2?  Exception: {0}", e.ToString());
        }
    }

    public void DeleteFile(GPhotoDeviceFile file)
    {
        files.Remove(file);
        fs.DeleteFile(file.Directory, file.Filename, context);
        file = null;
    }
    
    public CameraFile GetFile(GPhotoDeviceFile file)
    {
        file.CameraFile = fs.GetFile(file.Directory, file.Filename, CameraFileType.Normal, context);
        return file.CameraFile;
    }
    
    ~GPhotoDevice() {
        Dispose();
    }
    
    private bool disposed = false;
    
    public void Dispose() {
        if (!disposed) {
            disposed = true;
            //Console.WriteLine ("Disposing of gphotodevice: doing files");
            foreach(GPhotoDeviceFile file in files)
                file.Dispose();
            files = null;
            //Console.WriteLine ("dispose of files done. doing fs.");
            if (fs != null) 
                fs.Dispose();
            //Console.WriteLine ("dispose of fs done. doing camera.");
            if (camera != null)
                camera.Dispose();
            //Console.WriteLine ("dispose of camera done. doing context");
    
            context.Dispose();
            Console.WriteLine("dispose done.");
        } else {
            Console.WriteLine("already disposed");
        }
    }
}

}
