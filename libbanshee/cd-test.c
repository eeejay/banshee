#include <stdlib.h>
#include <stdio.h>
#include <glib.h>

#include "cd-detect.h"

void on_cd_added(const gchar *udi)
{
	g_printf("NEW AUDIO CD: %s\n", udi);
}

void on_device_removed(const gchar *udi)
{
	g_printf("REMOVED DEVICE: %s\n", udi);
}

gint main()
{
	GMainLoop *loop;
	
	loop = g_main_loop_new(g_main_context_default(), FALSE);
		
	if(!cd_detect_initialize()) {
		g_printf("Error: Could not initialize HAL\n");
		exit(1);
	}
	
	cd_detect_list_disks();
	cd_detect_set_device_added_callback(on_cd_added);
	cd_detect_set_device_removed_callback(on_device_removed);

	g_printf("Listening for Audio-CD-specific HAL events...\n");
	g_main_loop_run(loop);

	cd_detect_finalize();	

	exit(0);
}

