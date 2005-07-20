/***************************************************************************
 *  HalContext.cs
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
using System.Runtime.InteropServices;

namespace Hal
{
	public class Context : IDisposable
	{
		private HandleRef ctx_handle;
		private IntPtr dbus_conn;
		private bool initialized;
		private bool mainLoopIntegrated;
		
		public Context() : this(DbusBusType.System)
		{
		
		}
		
		protected Context(IntPtr dbus_conn)
		{
			IntPtr ctx = Unmanaged.libhal_ctx_new();
			if(ctx == IntPtr.Zero)
				throw new HalException("Could not create HAL Context"); 
			
			ctx_handle = new HandleRef(this, ctx);	
			DbusConnection = dbus_conn;
		}
		
		public Context(DbusBusType type) : 
			this(Unmanaged.dbus_bus_get(type, IntPtr.Zero))
		{
			Initialize();
		}
		
		public Context(DbusBusType type, bool initialize) : this(type)
		{
			Initialize();
		}
		
		~Context()
		{
			Cleanup();
		}
		
		public void Dispose()
		{
			Cleanup();
		}
		
		private void Cleanup()
		{
			ContextShutdown();
			ContextFree();
		}

		private bool ContextShutdown()
		{
			if(ctx_handle.Handle == IntPtr.Zero || !initialized)
				return false;
				
			return Unmanaged.libhal_ctx_shutdown(ctx_handle, IntPtr.Zero);
		}

		private bool ContextFree()
		{
			if(ctx_handle.Handle == IntPtr.Zero)
				return false;

			return Unmanaged.libhal_ctx_free(ctx_handle);
		}
		
		public void IntegrateMainLoop()
		{
			// would be nice to let user specify an optional GMainContext
			Unmanaged.dbus_connection_setup_with_g_main(dbus_conn,
				Unmanaged.g_main_context_default());
			
			mainLoopIntegrated = true;
		}
		
		public void Initialize()
		{
			if(!Unmanaged.libhal_ctx_init(ctx_handle, IntPtr.Zero))
				throw new HalException("Could not initialize HAL Context");
			
			initialized = true;
		}
		
		public IntPtr DbusConnection
		{
			set {
				dbus_conn = value;
				if(!Unmanaged.libhal_ctx_set_dbus_connection(ctx_handle, 
					dbus_conn))
					throw new HalException(
						"Could not set D-Bus Connection for HAL");
			}
		}
		
		public bool UseCache
		{
			set {
				if(!Unmanaged.libhal_ctx_set_cache(ctx_handle, value))
					throw new HalException("Could not set D-Bus Use Cache to '" 
						+ value + "'");
			}
		}
		
		public HandleRef Raw
		{
			get {
				return ctx_handle;
			}
		}
		
		// Events
		
		private Hashtable EventTable = new Hashtable();
		
		private bool AddEvent(Type evType, object ev)
		{
			if(EventTable[evType] == null)
				EventTable[evType] = new ArrayList();
				
			(EventTable[evType] as ArrayList).Add(ev);
			
			if(!mainLoopIntegrated)
				IntegrateMainLoop();
			
			return (EventTable[evType] as ArrayList).Count == 1;
		}
		
		private void RemoveEvent(Type evType, object ev)
		{
			if(EventTable[evType] == null)
				return;
				
			(EventTable[evType] as ArrayList).Remove(ev);
		}
		
		private ArrayList GetEvents(Type evType)
		{
			if(EventTable[evType] == null)
				return null;
				
			return EventTable[evType] as ArrayList;
		}
		
		public event DeviceAddedHandler DeviceAdded
		{
			add {
				if(AddEvent(typeof(DeviceAddedHandler), value))
					Unmanaged.libhal_ctx_set_device_added(ctx_handle,
						new DeviceAddedCallback(OnHalDeviceAdded));
			}
			
			remove {
				RemoveEvent(typeof(DeviceAddedHandler), value);
			}
		}
		
		public event DeviceRemovedHandler DeviceRemoved
		{
			add {
				if(AddEvent(typeof(DeviceRemovedHandler), value)) 
					Unmanaged.libhal_ctx_set_device_removed(ctx_handle,
						new DeviceRemovedCallback(OnHalDeviceRemoved));
			}
			
			remove {
				RemoveEvent(typeof(DeviceRemovedHandler), value);
			}
		}
		
		public event DeviceNewCapabilityHandler DeviceNewCapability
		{
			add {
				if(AddEvent(typeof(DeviceNewCapabilityHandler), value)) 
					Unmanaged.libhal_ctx_set_device_new_capability(ctx_handle,
						new DeviceNewCapabilityCallback(
							OnHalDeviceNewCapability));
			}
			
			remove {
				RemoveEvent(typeof(DeviceNewCapabilityHandler), value);
			}
		}
		
		public event DeviceLostCapabilityHandler DeviceLostCapability
		{
			add {
				if(AddEvent(typeof(DeviceLostCapabilityHandler), value)) 
					Unmanaged.libhal_ctx_set_device_lost_capability(ctx_handle,
						new DeviceLostCapabilityCallback(
							OnHalDeviceLostCapability));
			}
			
			remove {
				RemoveEvent(typeof(DeviceLostCapabilityHandler), value);
			}
		}
		
		public event DevicePropertyModifiedHandler DevicePropertyModified
		{
			add {
				if(AddEvent(typeof(DevicePropertyModifiedHandler), value)) 
					Unmanaged.libhal_ctx_set_device_property_modified(
						ctx_handle, new DevicePropertyModifiedCallback(
							OnHalDevicePropertyModified));
			}
			
			remove {
				RemoveEvent(typeof(DevicePropertyModifiedHandler), value);
			}
		}
		
		public event DeviceConditionHandler DeviceCondition
		{
			add {
				if(AddEvent(typeof(DeviceConditionHandler), value)) 
					Unmanaged.libhal_ctx_set_device_condition(ctx_handle, 
						new DeviceConditionCallback(OnHalDeviceCondition));
			}
			
			remove {
				RemoveEvent(typeof(DeviceConditionHandler), value);
			}
		}
	
		private void OnHalDeviceAdded(IntPtr ctx, IntPtr udiPtr)
		{
			foreach(DeviceAddedHandler addedHandler in 
				GetEvents(typeof(DeviceAddedHandler))) {
				DeviceAddedHandler handler = addedHandler;
				
				if(handler != null) {
					string udi = Marshal.PtrToStringAnsi(udiPtr);
					DeviceAddedArgs args = new DeviceAddedArgs();
					args.Device = new Device(this, udi);
					handler(this, args);
				}
			}
		}
		
		private void OnHalDeviceRemoved(IntPtr ctx, IntPtr udiPtr)
		{
			foreach(DeviceRemovedHandler removedHandler in 
				GetEvents(typeof(DeviceRemovedHandler))) {
				DeviceRemovedHandler handler = removedHandler;
				
				if(handler != null) {
					string udi = Marshal.PtrToStringAnsi(udiPtr);
					DeviceRemovedArgs args = new DeviceRemovedArgs();
					args.Device = new Device(this, udi);
					handler(this, args);
				}
			}
		}
		
		private void OnHalDeviceNewCapability(IntPtr ctx, IntPtr udiPtr, 
			IntPtr capPtr)
		{
			foreach(DeviceNewCapabilityHandler newCapHandler in 
				GetEvents(typeof(DeviceNewCapabilityHandler))) {
				DeviceNewCapabilityHandler handler = newCapHandler;
				
				if(handler != null) {
					string udi = Marshal.PtrToStringAnsi(udiPtr);
					string cap = Marshal.PtrToStringAnsi(capPtr);
					DeviceNewCapabilityArgs args = 
						new DeviceNewCapabilityArgs();
					args.Device = new Device(this, udi);
					args.Capability = cap;
					handler(this, args);
				}
			}
		}
		
		private void OnHalDeviceLostCapability(IntPtr ctx, IntPtr udiPtr, 
			IntPtr capPtr)
		{
			foreach(DeviceLostCapabilityHandler lostCapHandler in 
				GetEvents(typeof(DeviceLostCapabilityHandler))) {
				DeviceLostCapabilityHandler handler = lostCapHandler;
				
				if(handler != null) {
					string udi = Marshal.PtrToStringAnsi(udiPtr);
					string cap = Marshal.PtrToStringAnsi(capPtr);
					DeviceLostCapabilityArgs args = 
						new DeviceLostCapabilityArgs();
					args.Device = new Device(this, udi);
					args.Capability = cap;
					handler(this, args);
				}
			}
		}
		
		private void OnHalDevicePropertyModified(IntPtr ctx, IntPtr udiPtr, 
			IntPtr keyPtr, bool isRemoved, bool isAdded)
		{
			foreach(DevicePropertyModifiedHandler propModHandler in 
				GetEvents(typeof(DevicePropertyModifiedHandler))) {
				DevicePropertyModifiedHandler handler = propModHandler;
				
				if(handler != null) {
					string udi = Marshal.PtrToStringAnsi(udiPtr);
					string key = Marshal.PtrToStringAnsi(keyPtr);
					DevicePropertyModifiedArgs args = 
						new DevicePropertyModifiedArgs();
					args.Device = new Device(this, udi);
					args.Key = key;
					args.IsRemoved = isRemoved;
					args.IsAdded = isAdded;
					handler(this, args);
				}
			}
		}
		
		private void OnHalDeviceCondition(IntPtr ctx, IntPtr udiPtr, 
			IntPtr namePtr, IntPtr detailsPtr)
		{
			foreach(DeviceConditionHandler condHandler in 
				GetEvents(typeof(DeviceConditionHandler))) {
				DeviceConditionHandler handler = condHandler;
				
				if(handler != null) {
					string udi = Marshal.PtrToStringAnsi(udiPtr);
					string name = Marshal.PtrToStringAnsi(namePtr);
					string details = Marshal.PtrToStringAnsi(detailsPtr);
					DeviceConditionArgs args = new DeviceConditionArgs();
					args.Device = new Device(this, udi);
					args.ConditionName = name;
					args.ConditionDetails = details;
					handler(this, args);
				}
			}
		}
	}
}
