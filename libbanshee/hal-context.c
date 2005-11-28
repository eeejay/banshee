#include "hal-context.h"

static LibHalContext *global_instance_context = NULL;

LibHalContext *
hal_get_global_instance_context()
{
    if(global_instance_context == NULL) {
        global_instance_context = hal_context_new(NULL, NULL, NULL);
    }
    
    return global_instance_context;
}

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
    LibHalDeviceRemoved device_removed_cb)
{
	LibHalContext *hal_context;
	DBusError error;
	gchar **devices;
	gint device_count;

	*error_out = NULL;

    //g_debug("Trying to create context");

	hal_context = libhal_ctx_new();
	if(hal_context == NULL) {
		*error_out = g_strdup("Could not create new HAL context");
		g_warning(*error_out);
		return NULL;
	}
	
	dbus_error_init(&error);
	if(!hal_mainloop_integrate(hal_context, g_main_context_default(), 
		&error)) {
		dbus_error_free(&error);
		libhal_ctx_free(hal_context);
		*error_out = g_strdup_printf("Could not integrate HAL with mainloop: %s", error.message);
		g_warning(*error_out);
		return NULL;
	}
	
	if(device_added_cb != NULL) {
	   libhal_ctx_set_device_added(hal_context, device_added_cb);
	}
	
	if(device_removed_cb != NULL) {
	   libhal_ctx_set_device_removed(hal_context, device_removed_cb);
	}
	
	if(!libhal_ctx_init(hal_context, &error)) {
		libhal_ctx_free(hal_context);
		if(dbus_error_is_set(&error)) {
			*error_out = g_strdup_printf("Could not initialize HAL context: %s", error.message);
			g_warning(*error_out);
			dbus_error_free(&error);
		} else {
			*error_out = g_strdup_printf("Could not initialize HAL context");
			g_warning(*error_out);
		}
		return NULL;
	}

	devices = libhal_get_all_devices(hal_context, &device_count, NULL);
	if(devices == NULL) {
		libhal_ctx_shutdown(hal_context, NULL);
		libhal_ctx_free(hal_context);
		*error_out = g_strdup("Could not get device list from HAL");
		g_warning(*error_out);
		hal_context = NULL;
		return NULL;
	}
	
	libhal_free_string_array(devices);

    //g_debug("Context created");
	
	return hal_context;
}

void
hal_context_free(LibHalContext *ctx)
{
	libhal_ctx_shutdown(ctx, NULL);
	libhal_ctx_free(ctx);
	ctx = NULL;
}
