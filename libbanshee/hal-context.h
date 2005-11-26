
#ifndef HAL_CONTEXT_H
#define HAL_CONTEXT_H

#include <libhal.h>
#include <dbus/dbus.h>
#include <dbus/dbus-glib.h>

LibHalContext *hal_context_new(gchar **error_out, 
    LibHalDeviceAdded device_added_cb, 
    LibHalDeviceRemoved device_removed_cb);
void hal_context_free(LibHalContext *ctx);

#endif /* HAL_CONTEXT_H */
