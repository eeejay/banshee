#ifdef HAVE_CONFIG_H
#  include <config.h>
#endif

#include <stdlib.h>
#include <stdio.h>
#include <glib/gstdio.h>
#include <glib.h>
#include <libbonobo.h>
#include <gnome.h>

#include "GNOME_Media_CDDBSlave2.h"
#include "cddb-slave-client.h"

#define DISK_ID "8208d90b"
#define OFFSETS "150 10266 27890 43170 61910 79521 95330 110453 128264 139036 155229"
#define N_TRACKS 11
#define TOTAL_LENGTH 2267

static void
cddb_slave_listener_event_cb(BonoboListener *listener, const gchar *name,
	const BonoboArg *arg, CORBA_Environment *ev, gpointer data)
{
	GNOME_Media_CDDBSlave2_QueryResult * query = arg->_value;
	CDDBSlaveClientTrackInfo **track_info = NULL;
	CDDBSlaveClient *cddb_client = (CDDBSlaveClient *)data;
	int ntracks, i;
	GList *scan;

	if(query->result != GNOME_Media_CDDBSlave2_OK)
		return;

	if(!cddb_slave_client_is_valid(cddb_client, query->discid))
		return;

	g_printf("Disc Artist: %s\n", cddb_slave_client_get_artist(cddb_client,
		query->discid));
	g_printf("Disc Title: %s\n", cddb_slave_client_get_disc_title(cddb_client,
		query->discid));
}

int main(int argc, char **argv)
{
	CDDBSlaveClient *cddb_client = NULL;
	BonoboListener *listener = NULL;
	GnomeProgram *program;

	program = gnome_program_init(PACKAGE, VERSION,
		LIBGNOMEUI_MODULE, argc, argv, GNOME_PARAM_POPT_TABLE,
		NULL, GNOME_PARAM_NONE);
	

	cddb_client = cddb_slave_client_new();
	listener = bonobo_listener_new(NULL, NULL);
	g_signal_connect(G_OBJECT(listener), "event-notify", 
		G_CALLBACK(cddb_slave_listener_event_cb),
		cddb_client);
	cddb_slave_client_add_listener(cddb_client, listener);

	cddb_slave_client_query(cddb_client, DISK_ID, N_TRACKS, 
		OFFSETS, TOTAL_LENGTH, PACKAGE, VERSION);

	//bonobo_main();

	gtk_main();

	exit(0);
}

