/* ex: set ts=4: */
/***************************************************************************
*  cd-detect.c
*  Copyright (C) 2005 Novell 
*  Written by Aaron Bockover <aaron@aaronbock.net>
****************************************************************************/

/*  
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of version 2.1 of the GNU Lesser General Public
 *  License as published by the Free Software Foundation.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Lesser Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307
 *  USA
 */

#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>
#include <string.h>

#include "cd-detect.h"

static LibHalContext *hal_ctx = NULL;
static CdDetectUdiCallback on_device_added = NULL;
static CdDetectUdiCallback on_device_removed = NULL;

dbus_bool_t 
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

static void
hal_device_added(LibHalContext *ctx, const char *udi)
{
	if(!libhal_device_get_property_bool(ctx, udi, "volume.disc.has_audio", 
		NULL)) {
		return;
	}
	
	if(on_device_added != NULL)
		on_device_added(udi);
}

static void
hal_device_removed(LibHalContext *ctx __attribute__((__unused__)), 
	const char *udi)
{
	if(on_device_removed != NULL)
		on_device_removed(udi);
}

LibHalContext *
cd_detect_hal_initialize()
{
	LibHalContext *hal_context;
	DBusError error;
	gchar **devices;
	gint device_count;

	hal_context = libhal_ctx_new();
	if(hal_context == NULL) 
		return NULL;
	
	dbus_error_init(&error);
	if(!hal_mainloop_integrate(hal_context, g_main_context_default(), 
		&error)) {
		dbus_error_free(&error);
		libhal_ctx_free(hal_context);
		return NULL;
	}
	
	libhal_ctx_set_device_added(hal_context, hal_device_added);
	libhal_ctx_set_device_removed(hal_context, hal_device_removed);
	
	if(!libhal_ctx_init(hal_context, &error)) {
		libhal_ctx_free(hal_context);
		return NULL;
	}

	devices = libhal_get_all_devices(hal_context, &device_count, NULL);
	if(devices == NULL) {
		libhal_ctx_shutdown(hal_context, NULL);
		libhal_ctx_free(hal_context);
		hal_context = NULL;
		return NULL;
	}
	
	libhal_free_string_array(devices);
	
	return hal_context;
}

DiskInfo *
cd_detect_get_disk_info(gchar *udi)
{
	DiskInfo *disk;
	gchar **volumes;
	gint volume_count;
	gchar *disk_udi;
	dbus_bool_t has_audio;
	
	if(hal_ctx == NULL)
		return NULL;
		
	volumes = libhal_manager_find_device_string_match(hal_ctx,
		"info.parent", udi, &volume_count, NULL);
			
	if(volume_count < 1) {
		libhal_free_string_array(volumes);
		return NULL;
	}
	
	disk_udi = volumes[0];
	
	has_audio = libhal_device_get_property_bool(hal_ctx, disk_udi, 
		"volume.disc.has_audio", NULL);
	
	if(!has_audio || !libhal_device_property_exists(hal_ctx, disk_udi, 
		"block.device", NULL) || !libhal_device_property_exists(hal_ctx, udi, 
		"info.product", NULL)) {
		libhal_free_string_array(volumes);
		return NULL;
	}
	
	disk = g_new0(DiskInfo, 1);
	if(disk == NULL) {	
		libhal_free_string_array(volumes);
		return NULL;
	}
	
	disk->device_node = g_strdup(libhal_device_get_property_string(hal_ctx, 
		disk_udi, "block.device", NULL));
	disk->drive_name = g_strdup(libhal_device_get_property_string(hal_ctx, 
		udi, "info.product", NULL));
	disk->udi = g_strdup(disk_udi);
	
	libhal_free_string_array(volumes);
	return disk;
}

/* PUBLIC FUNCTIONS */

gboolean
cd_detect_initialize()
{
	if(hal_ctx == NULL) 
		hal_ctx = cd_detect_hal_initialize();
		
	return hal_ctx != NULL;
}

void
cd_detect_finalize()
{
	if(hal_ctx == NULL)
		return;
		
	libhal_ctx_shutdown(hal_ctx, NULL);
	libhal_ctx_free(hal_ctx);
	hal_ctx = NULL;
}

void
cd_detect_disk_info_free(DiskInfo *disk)
{
	if(disk == NULL)
		return;
		
	g_free(disk->udi);
	g_free(disk->device_node);
	g_free(disk->drive_name);
	
	g_free(disk);
	disk = NULL;
}

GList *
cd_detect_list_disks()
{
	gchar **devices;
	gint device_count, i;
	DiskInfo *disk = NULL;
	GList *disks = NULL;
	
	if(hal_ctx == NULL)
		return NULL;
	
	devices = libhal_manager_find_device_string_match(hal_ctx, 
		"storage.drive_type", "cdrom", &device_count, NULL);
	
	for(i = 0; i < device_count; i++) {
		disk = cd_detect_get_disk_info(devices[i]);
		if(disk == NULL)
			continue;

		disks = g_list_append(disks, disk);
	}
		
	libhal_free_string_array(devices);
		
	return disks;
}

gboolean
cd_detect_set_device_added_callback(CdDetectUdiCallback cb)
{
	if(on_device_added != NULL)
		return FALSE;
		
	on_device_added = cb;
	return TRUE;
}

gboolean
cd_detect_set_device_removed_callback(CdDetectUdiCallback cb)
{
	if(on_device_removed != NULL)
		return FALSE;
		
	on_device_removed = cb;
	return TRUE;
}
