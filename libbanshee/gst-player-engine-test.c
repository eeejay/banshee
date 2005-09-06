#include <stdlib.h>
#include <stdio.h>
#include <glib.h>

#include "gst-player-engine.h"

GstPlayerEngine *engine;

void on_engine_iterate(void *engine, int position, int length)
{
	//g_printf("Iterate: %d, %d\n", position, length);
}

void on_error(void *engine, const gchar *message)
{
	g_printf("Error: %s, %s, %d\n", message, 
		gpe_get_error((GstPlayerEngine *)engine), 
		gpe_have_error((GstPlayerEngine *)engine));
}

void on_eos()
{
	g_printf("End Of Stream Reached\n");
}

gboolean set_pos_timeout()
{
	//gpe_set_position(engine, 80);
	return FALSE;
}

int main()
{
	GMainLoop *loop;
	GMainContext *loop_context;

	g_type_init();
	
	loop_context = g_main_context_default();	
	loop = g_main_loop_new(loop_context, FALSE);
		
	engine = gpe_new();
	gpe_set_iterate_handler(engine, on_engine_iterate);
	gpe_set_error_handler(engine, on_error);
	gpe_set_end_of_stream_handler(engine, on_eos);
	
	gpe_open(engine, "cdda://4#/dev/hdc");
	gpe_set_volume(engine, 90);
	gpe_play(engine);

	gpe_open(engine, "cdda://8#/dev/hcc");
	gpe_play(engine);
	
	
	g_timeout_add(5000, (GSourceFunc)set_pos_timeout, NULL);

	
	g_main_loop_run(loop);
}
