/* ex: set ts=4: */
/***************************************************************************
 *  hal-context-c
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

#include "hal-context.h"

static dbus_bool_t 
hal_mainloop_integrate(LibHalContext *ctx, GMainContext *mainctx, 
	DBusError *error)
{
	DBusConnection *dbus_connection = dbus_bus_get(DBUS_BUS_SYSTEM, error);
	
	if(dbus_error_is_set(error))
		return FALSE;
	
	dbus_connection_setup_with_g_main(dbus_connection, mainctx);
	libhal_ctx_set_dbus_connection(ctx, dbus_connection);
	
	return TRUE;
}

LibHalContext *
hal_context_new(gchar **error_out, LibHalDeviceAdded device_added_cb, 
    LibHalDeviceRemoved device_removed_cb, 
    LibHalDevicePropertyModified device_property_modified_cb)
{
	LibHalContext *hal_context;
	DBusError error;
	gchar **devices;
	gint device_count;

	*error_out = NULL;

	hal_context = libhal_ctx_new();
	if(hal_context == NULL) {
		*error_out = g_strdup(_("Could not create new HAL context"));
		return NULL;
	}
	
	dbus_error_init(&error);
	if(!hal_mainloop_integrate(hal_context, g_main_context_default(), 
		&error)) {
		libhal_ctx_free(hal_context);
		*error_out = g_strdup_printf(_("Could not integrate HAL with mainloop: %s"), error.message);
		dbus_error_free(&error);
		return NULL;
	}
	
	if(device_added_cb != NULL) {
	   libhal_ctx_set_device_added(hal_context, device_added_cb);
	}
	
	if(device_removed_cb != NULL) {
	   libhal_ctx_set_device_removed(hal_context, device_removed_cb);
	}
	
	if(device_property_modified_cb != NULL) {
	   libhal_ctx_set_device_property_modified(hal_context, device_property_modified_cb);
    }
	
	if(!libhal_ctx_init(hal_context, &error)) {
		libhal_ctx_free(hal_context);
		if(dbus_error_is_set(&error)) {
			*error_out = g_strdup_printf("%s: %s", _("Could not initialize HAL context"), error.message);
			dbus_error_free(&error);
		} else {
			*error_out = g_strdup_printf(_("Could not initialize HAL context"));
		}
		return NULL;
	}

	devices = libhal_get_all_devices(hal_context, &device_count, NULL);
	if(devices == NULL) {
		libhal_ctx_shutdown(hal_context, NULL);
		libhal_ctx_free(hal_context);
		*error_out = g_strdup(_("Could not get device list from HAL"));
		hal_context = NULL;
		return NULL;
	}
	
	libhal_free_string_array(devices);
	
	return hal_context;
}

void
hal_context_free(LibHalContext *ctx)
{
	libhal_ctx_shutdown(ctx, NULL);
	libhal_ctx_free(ctx);
	ctx = NULL;
}
