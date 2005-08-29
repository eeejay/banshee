/* ex: set ts=4: */
/***************************************************************************
 *  cd-detect.h
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
 
#ifndef CD_DETECT_H
#define CD_DETECT_H

#include <glib.h>
#include <glib/gstdio.h>
#include <libhal.h>
#include <dbus/dbus.h>
#include <dbus/dbus-glib.h>

typedef void (* CdDetectUdiCallback) (const gchar *udi);

typedef struct {
	LibHalContext *hal_ctx;
	CdDetectUdiCallback on_device_added;
	CdDetectUdiCallback on_device_removed;
} CdDetect;

typedef struct {
	gchar *udi;
	gchar *device_node;
	gchar *drive_name;
} DiskInfo;

CdDetect *cd_detect_new();
void cd_detect_free(CdDetect *cd_detect);
DiskInfo **cd_detect_get_disk_array(CdDetect *cd_detect);
void cd_detect_disk_array_free(DiskInfo **disk_array);
gboolean cd_detect_set_device_added_callback(CdDetect *cd_detect, 
	CdDetectUdiCallback cb);
gboolean cd_detect_set_device_removed_callback(CdDetect *cd_detect, 
	CdDetectUdiCallback cb);
void cd_detect_print_disk_info(DiskInfo *disk);

#endif /* CD_DETECT_H */
