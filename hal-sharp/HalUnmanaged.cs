/***************************************************************************
 *  HalUnmanaged.cs
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
using System.Runtime.InteropServices;

namespace Hal
{
	internal sealed class Unmanaged
	{
		// Context Functions
		
		[DllImport("libhal")]
		public static extern IntPtr libhal_ctx_new();

		[DllImport("libhal")]
		public static extern IntPtr libhal_ctx_init_direct(IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_ctx_set_dbus_connection(HandleRef ctx, 
			IntPtr conn);
			
		[DllImport("libhal")]
		public static extern bool libhal_ctx_set_cache(HandleRef ctx, 
			bool use_cache);

		[DllImport("libhal")]
		public static extern bool libhal_ctx_init(HandleRef ctx, IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_ctx_shutdown(HandleRef ctx, 
			IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_ctx_free(HandleRef ctx);

		// Callback Set Functions

		[DllImport("libhal")]
		public static extern bool libhal_ctx_set_device_added(HandleRef ctx,
			DeviceAddedCallback cb);

		[DllImport("libhal")]
		public static extern bool libhal_ctx_set_device_removed(HandleRef ctx,
			DeviceRemovedCallback cb);
			
		[DllImport("libhal")]
		public static extern bool libhal_ctx_set_device_new_capability(
			HandleRef ctx, DeviceNewCapabilityCallback cb);

		[DllImport("libhal")]
		public static extern bool libhal_ctx_set_device_lost_capability(
			HandleRef ctx, DeviceLostCapabilityCallback cb);
			
		[DllImport("libhal")]
		public static extern bool libhal_ctx_set_device_property_modified(
			HandleRef ctx, DevicePropertyModifiedCallback cb);
		
		[DllImport("libhal")]
		public static extern bool libhal_ctx_set_device_condition(
			HandleRef ctx, DeviceConditionCallback cb);
			
		// String Functions

		[DllImport("libhal")]
		public static extern void libhal_free_string_array(IntPtr array);

		[DllImport("libhal")]
		public static extern void libhal_free_string(IntPtr str);
		
		// Device Functions
		
		[DllImport("libhal")]
		public static extern IntPtr libhal_get_all_devices(HandleRef ctx,
			out int count, IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_device_exists(HandleRef ctx, 
			string udi, IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_device_print(HandleRef ctx, string udi,
			IntPtr error);

		// Property Get Functions

		[DllImport("libhal")]
		public static extern IntPtr libhal_device_get_property_string(
			HandleRef ctx, string udi, string key, IntPtr error);

		[DllImport("libhal")]
		public static extern int libhal_device_get_property_int(HandleRef ctx,
			string udi, string key, IntPtr error);

		[DllImport("libhal")]
		public static extern UInt64 libhal_device_get_property_uint64(
			HandleRef ctx, string udi, string key, IntPtr error);
			
		[DllImport("libhal")]
		public static extern double libhal_device_get_property_double(
			HandleRef ctx, string udi, string key, IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_device_get_property_bool(HandleRef ctx,
			string udi, string key, IntPtr error);

		// Property Set Functions
			
		[DllImport("libhal")]
		public static extern bool libhal_device_set_property_string(
			HandleRef ctx, string udi, string key, string value, IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_device_set_property_int(HandleRef ctx,
			string udi, string key, int value, IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_device_set_property_uint64(
			HandleRef ctx, string udi, string key, UInt64 value, IntPtr error);
			
		[DllImport("libhal")]
		public static extern bool libhal_device_set_property_double(
			HandleRef ctx, string udi, string key, double value, IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_device_set_property_bool(HandleRef ctx,
			string udi, string key, bool value, IntPtr error);
		
		// String List Property Functions
		
		[DllImport("libhal")]
		public static extern IntPtr libhal_device_get_property_strlist(
			HandleRef ctx, string udi, string key, IntPtr error);
			
		[DllImport("libhal")]
		public static extern bool libhal_device_property_strlist_append(
			HandleRef ctx, string udi, string key, string value, IntPtr error);
			
		[DllImport("libhal")]
		public static extern bool libhal_device_property_strlist_prepend(
			HandleRef ctx, string udi, string key, string value, IntPtr error);
			
		[DllImport("libhal")]
		public static extern bool libhal_device_property_strlist_remove_index(
			HandleRef ctx, string udi, string key, uint index, IntPtr error);
			
		[DllImport("libhal")]
		public static extern bool libhal_device_property_strlist_remove(
			HandleRef ctx, string udi, string key, string value, IntPtr error);

		// Other Property Functions
		
		[DllImport("libhal")]
		public static extern bool libhal_device_property_exists(HandleRef ctx,
			string udi, string key, IntPtr error);
			
		[DllImport("libhal")]
		public static extern PropertyType libhal_device_get_property_type(
			HandleRef ctx, string udi, string key, IntPtr error);

		[DllImport("libhal")]
		public static extern bool libhal_device_remove_property(HandleRef ctx,
			string udi, string key, IntPtr error);
			
		[DllImport("libhal")]
		public static extern IntPtr libhal_manager_find_device_string_match(
			HandleRef ctx, string key, string value, out int num_devices,
			IntPtr error);
			
		// Capability Functions
		
		[DllImport("libhal")]
		public static extern bool libhal_device_add_capability(HandleRef ctx,
			string udi, string capability, IntPtr error);		
		
		[DllImport("libhal")]
		public static extern bool libhal_device_query_capability(HandleRef ctx,
			string udi, string capability, IntPtr error);		
		
		[DllImport("libhal")]
		public static extern IntPtr libhal_find_device_by_capability(
			HandleRef ctx, string capability, out int num_deivces, 
			IntPtr error);
		
		[DllImport("libhal")]
		public static extern bool libhal_device_property_watch_all(
			HandleRef ctx, IntPtr error);	
			
		[DllImport("libhal")]
		public static extern bool libhal_device_add_property_watch(
			HandleRef ctx, string udi, IntPtr error);		
		
		[DllImport("libhal")]
		public static extern bool libhal_device_remove_property_watch(
			HandleRef ctx, string udi, IntPtr error);		
		
		// Locking Functions
		
		[DllImport("libhal")]
		public static extern bool libhal_device_lock(HandleRef ctx, string udi,
			string reason_to_lock, out string reason_why_locked, IntPtr error);		
		
		[DllImport("libhal")]
		public static extern bool libhal_device_unlock(HandleRef ctx,
			string udi, IntPtr error);		
		
		// Rescan/Reprobe Functions
		
		[DllImport("libhal")]
		public static extern bool libhal_device_rescan(HandleRef ctx,
			string udi, IntPtr error);		
		
		[DllImport("libhal")]
		public static extern bool libhal_device_reprobe(HandleRef ctx,
			string udi, IntPtr error);		

		// Condition Functions
		
		[DllImport("libhal")]
		public static extern bool libhal_device_emit_condition(HandleRef ctx,
			string udi, string condition_name, string condition_details,
			IntPtr error);
			
		// D-Bus Functions
		
		[DllImport("libdbus-1")]
		public static extern IntPtr dbus_bus_get(DbusBusType bus_typed, 
			IntPtr error);
		
		[DllImport("libdbus-glib-1")]
		public static extern void dbus_connection_setup_with_g_main(
			IntPtr dbus_connection, IntPtr g_main_context);
			
		// ughhh
		[DllImport("libglib-2.0.so")]
		public static extern IntPtr g_main_context_default();
		
	}
}
