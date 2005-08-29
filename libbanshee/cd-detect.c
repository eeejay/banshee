/* ex: set ts=4: */
/***************************************************************************
 *  cd-detect.c
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

#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>
#include <string.h>

#include "cd-detect.h"

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
	CdDetect *cd_detect = (CdDetect *)libhal_ctx_get_user_data(ctx);
	
	if(cd_detect == NULL || !libhal_device_get_property_bool(ctx, 
		udi, "volume.disc.has_audio", NULL)) {
		return;
	}
	
	if(cd_detect->on_device_added != NULL)
		cd_detect->on_device_added(udi);
}

static void
hal_device_removed(LibHalContext *ctx, const char *udi)
{
	CdDetect *cd_detect = (CdDetect *)libhal_ctx_get_user_data(ctx);
	
	if(cd_detect == NULL)
		return;
		
	if(cd_detect->on_device_removed != NULL)
		cd_detect->on_device_removed(udi);
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
cd_detect_get_disk_info(LibHalContext *hal_ctx, gchar *udi)
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

/* PUBLIC FUNCTIONS */

CdDetect *
cd_detect_new()
{
	CdDetect *cd_detect = NULL;
	LibHalContext *hal_ctx = cd_detect_hal_initialize();
	
	if(hal_ctx == NULL)
		return NULL;

	cd_detect = g_new0(CdDetect, 1);
	cd_detect->hal_ctx = hal_ctx;
	cd_detect->on_device_added = NULL;
	cd_detect->on_device_removed = NULL;
	
	libhal_ctx_set_user_data(cd_detect->hal_ctx, cd_detect);
	
	return cd_detect;
}

void
cd_detect_free(CdDetect *cd_detect)
{
	if(cd_detect == NULL)
		return;
		
	libhal_ctx_shutdown(cd_detect->hal_ctx, NULL);
	libhal_ctx_free(cd_detect->hal_ctx);
	cd_detect->hal_ctx = NULL;
	
	g_free(cd_detect);
	cd_detect = NULL;
}

DiskInfo **
cd_detect_get_disk_array(CdDetect *cd_detect)
{
	gchar **devices;
	gint device_count, i, n;
	DiskInfo *disk = NULL;
	GList *disks = NULL;
	DiskInfo **disk_array = NULL;
	
	if(cd_detect == NULL)
		return NULL;
	
	devices = libhal_manager_find_device_string_match(cd_detect->hal_ctx, 
		"storage.drive_type", "cdrom", &device_count, NULL);
	
	for(i = 0; i < device_count; i++) {
		disk = cd_detect_get_disk_info(cd_detect->hal_ctx, devices[i]);
		if(disk == NULL)
			continue;

		disks = g_list_append(disks, disk);	
	}
		
	libhal_free_string_array(devices);
	
	n = g_list_length(disks);
	disk_array = (DiskInfo **)g_new0(DiskInfo, n + 1);
	for(i = 0; i < n; i++)
		disk_array[i] = g_list_nth_data(disks, i);
	disk_array[n] = NULL;
		
	g_list_free(disks);
		
	return disk_array;
}

void
cd_detect_disk_array_free(DiskInfo **disk_array)
{
	gint i;
	
	if(disk_array == NULL)
		return;
	
	for(i = 0; disk_array[i] != NULL; i++)
		cd_detect_disk_info_free(disk_array[i]); 
		
	g_free(disk_array);
	disk_array = NULL;
}

gboolean
cd_detect_set_device_added_callback(CdDetect *cd_detect, 
	CdDetectUdiCallback cb)
{
	if(cd_detect == NULL)
		return FALSE;
		
	cd_detect->on_device_added = cb;
	return TRUE;
}

gboolean
cd_detect_set_device_removed_callback(CdDetect *cd_detect,
	CdDetectUdiCallback cb)
{
	if(cd_detect == NULL)
		return FALSE;
		
	cd_detect->on_device_removed = cb;
	return TRUE;
}

void
cd_detect_print_disk_info(DiskInfo *disk)
{
	g_printf("UDI:         %s\n", disk->udi);
	g_printf("Device Node: %s\n", disk->device_node);
	g_printf("Drive Name:  %s\n", disk->drive_name);
}

