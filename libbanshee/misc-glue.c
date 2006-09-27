#include <gtk/gtk.h>
#include <dbus/dbus-glib.h>

typedef gboolean (* activate_handler)(GtkCellRenderer *cell,
	GdkEvent *event, 
	GtkWidget *widget,
	const gchar *path,
	GdkRectangle *background_area,
	GdkRectangle *cell_area,
	GtkCellRendererState flags);

void gtksharp_cell_renderer_activatable_configure(GtkCellRenderer *renderer, activate_handler *handler)
{
	GTK_CELL_RENDERER_GET_CLASS(renderer)->activate = handler;
	renderer->mode = GTK_CELL_RENDERER_MODE_ACTIVATABLE;
}

void banshee_dbus_compat_thread_init()
{
	dbus_g_thread_init();
}

