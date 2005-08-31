/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  NautilusBurnUtil.cs
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
using System.Text.RegularExpressions;
 
namespace Nautilus 
{
	public class BurnUtil
	{
		private static BurnDrive [] driveArray;
	
		public static string GetDriveUniqueId(BurnDrive drive)
		{		
			return Regex.Replace(drive.DisplayName + "-" + 
				drive.CdRecordId, @"[^0-9A-Za-z]+", "-").ToLower();
		}
		
		public static BurnDrive [] GetDrives()
		{
			return GetDrives(false);
		}
		
		public static BurnDrive [] GetDrives(bool reload)
		{
			if(reload || driveArray == null || driveArray.Length == 0) {	
				GLib.List driveList = BurnDrive.GetList(true, false);
				
				if(driveList == null || driveList.Count == 0) {
					driveArray = null;
					return null;
				}
				
				driveArray = new BurnDrive[driveList.Count];
				
				for(int i = 0; i < driveList.Count; i++)
					driveArray[i] = ((BurnDrive)driveList[i]).Copy();
			}
				
			return driveArray;
		}
		
		public static BurnDrive GetDriveById(string id, bool reload)
		{
			BurnDrive [] drives = GetDrives(reload);
			
			foreach(BurnDrive drive in drives)
				if(GetDriveUniqueId(drive).Equals(id))
					return drive;
					
			return null;	
		}
		
		public static BurnDrive GetDriveByIdOrDefault(string id)
		{
			BurnDrive drive = GetDriveById(id, false);
			
			if(drive != null)
				return drive;
				
			BurnDrive [] drives = GetDrives(false);
			
			if(drives == null)
				return null;
				
			return drives[0];
		}
	}
}
