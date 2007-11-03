include $(top_srcdir)/build/Common.Makefile

HYENA_GUI = Hyena.Gui
HYENA_GUI_ASSEMBLY = $(HYENA_GUI).dll

hyenadir = $(pkglibdir)
hyena_SCRIPTS = $(HYENA_GUI_ASSEMBLY) $(HYENA_GUI_ASSEMBLY).mdb

HYENA_GUI_SOURCE = \
	$(wildcard Hyena.Data.Gui/*.cs) \
	$(wildcard Hyena.Gui.Dialogs/*.cs) \
	$(wildcard Hyena.Gui/*.cs)
HYENA_GUI_SOURCE_BUILD = $(addprefix $(srcdir)/, $(HYENA_GUI_SOURCE))

all: $(HYENA_GUI_ASSEMBLY)

$(HYENA_GUI_ASSEMBLY): $(HYENA_GUI_SOURCE_BUILD)
	$(BUILD_LIB) -out:$@ $(LINK_HYENA) $(LINK_CAIRO) $(LINK_GTK) $(LINK_MONO_UNIX) $(HYENA_GUI_SOURCE_BUILD)

EXTRA_DIST = \
	$(HYENA_GUI_SOURCE)

CLEANFILES = $(HYENA_GUI_ASSEMBLY) *.mdb *.dll *.pidb
MAINTAINERCLEANFILES = Makefile.in

