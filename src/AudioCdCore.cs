/***************************************************************************
 *  AudioCdCore.cs
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
using Hal;

namespace Banshee
{
	public class AudioCdCore
	{
		private static AudioCdCore instance;
		private Hashtable disks;
		
		public event EventHandler Updated;
	
		public static AudioCdCore Instance
		{
			get {
				if(instance == null) 
					instance = new AudioCdCore();
				return instance;
			}
		}
	
		public AudioCdCore()
		{
			Context ctx = new Context();
			ctx.DeviceAdded += OnHalDeviceAdded;
			ctx.DeviceRemoved += OnHalDeviceRemoved;
			ctx.DeviceCondition += OnHalDeviceCondition;
			ctx.DeviceLostCapability += OnHalDeviceLostCapability;
			ctx.DeviceNewCapability += OnHalDeviceNewCapability;
			ctx.DevicePropertyModified += OnHalDevicePropertyModified;
		}
	
		public void OnHalDeviceAdded(object o, DeviceAddedArgs args)
		{
			Console.WriteLine("Device Added: " + args.Device);
			args.Device.WatchProperties = true;
		}
		
		public void OnHalDeviceRemoved(object o, DeviceRemovedArgs args)
		{
			Console.WriteLine("Device Removed: " + args.Device);
		}
		
		public void OnHalDeviceCondition(object o, DeviceConditionArgs args)
		{
			Console.WriteLine("Device Condition: " + args.Device);
		}
		
		public void OnHalDeviceLostCapability(object o, 
			DeviceLostCapabilityArgs args)
		{
			Console.WriteLine("Device Lost Capability: " + args.Device);	
		}
		
		public void OnHalDeviceNewCapability(object o,
			DeviceNewCapabilityArgs args)
		{
			Console.WriteLine("Device New Capability: " + args.Device);
		}
		
		public void OnHalDevicePropertyModified(object o,
			DevicePropertyModifiedArgs args)
		{
			Console.WriteLine("Device Property Modified: " + args.Device +
				", Key: " + args.Key + ", Is Removed: " + args.IsRemoved +
				", Is Added: " + args.IsAdded);
		}
		
		private void OnDeviceAdded(object o, EventArgs args)
		{
		}
		
		private void OnDeviceRemoved(object o, EventArgs args)
		{
		}
		
		public void ListAll()
		{

		}
		
		private void HandleUpdated()
		{
			EventHandler handler = Updated;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		public string [] Disks
		{
			get {
				ArrayList list = new ArrayList(disks.Values);
				return list.ToArray(typeof(string)) as string [];
			}
		}
	}
}
