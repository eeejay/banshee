/* ex: set ts=4: */
/***************************************************************************
*  cd-detect.h
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

#ifndef CD_DETECT_H
#define CD_DETECT_H

#include <glib.h>
#include <glib/gstdio.h>
#include <libhal.h>
#include <dbus/dbus.h>
#include <dbus/dbus-glib.h>

typedef void (* CdDetectUdiCallback) (const gchar *udi);

typedef struct {
	gchar *udi;
	gchar *device_node;
	gchar *drive_name;
} DiskInfo;

gboolean cd_detect_initialize();
void cd_detect_finalize();
GList *cd_detect_list_disks();
gboolean cd_detect_set_device_added_callback(CdDetectUdiCallback cb);
gboolean cd_detect_set_device_removed_callback(CdDetectUdiCallback cb);

#endif /* CD_DETECT_H */
