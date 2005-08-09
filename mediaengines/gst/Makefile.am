INCLUDES = $(GST_CFLAGS)

MCS_FLAGS = -debug -nowarn:0169
ASSEMBLY_NAME = gst-player
ASSEMBLY = $(ASSEMBLY_NAME).dll

gstenginelibdir = $(pkglibdir)/mediaengines/gst
gstenginelib_LTLIBRARIES = libgstmediaengine.la
gstenginelib_SCRIPTS = $(ASSEMBLY)

libgstmediaengine_la_SOURCES = \
	gst-encode.c \
	gst-encode.h \
	gst-player-engine.c \
	gst-player-engine.h

libgstmediaengine_la_LIBADD = $(GST_LIBS)

ASSEMBLY_SOURCES = $(srcdir)/GstPlayerEngine.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) -target:library -out:$@ $(GTKSHARP_LIBS) -r:$(top_builddir)/plugincore/plugin-core.dll $(ASSEMBLY_SOURCES)

DISTCLEANFILES = Makefile.in
CLEANFILES = gst-encode gst-play  *.exe *.dll *.mdb *.pidb

EXTRA_DIST = gst-encode-test.c gst-player-engine-test.c $(ASSEMBLY_SOURCES)

test:
	$(CC) $(GST_LIBS) $(GST_CFLAGS) -o gst-encode gst-encode.c \
		gst-encode-test.c 
	$(CC) $(GST_LIBS) $(GST_CFLAGS) -o gst-play gst-player-engine.c \
		gst-player-engine-test.c
