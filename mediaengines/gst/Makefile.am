INCLUDES = $(GST_CFLAGS)

MCS_FLAGS = -debug -nowarn:0169
ASSEMBLY_NAME = gst-player
ASSEMBLY = $(ASSEMBLY_NAME).dll

gstenginelibdir = $(pkglibdir)/mediaengines/gst
gstenginelib_LTLIBRARIES = libgstmediaengine.la
gstenginelib_SCRIPTS = gst-sharp.dll $(ASSEMBLY)

libgstmediaengine_la_SOURCES = \
	gst-encode.c \
	gst-encode.h

libgstmediaengine_la_LIBADD = $(GST_LIBS)

ASSEMBLY_SOURCES = GstPlayer.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) -target:library -out:$@ -r:$(top_builddir)/plugincore/plugin-core.dll -r:gst-sharp.dll $(GTKSHARP_LIBS) $(ASSEMBLY_SOURCES)

DISTCLEANFILES = Makefile.in
CLEANFILES = gst-encode *.dll *.mdp *.exe

EXTRA_DIST = gst-encode-test.c gst-sharp.dll $(ASSEMBLY_SOURCES)

test:
	$(CC) $(GST_LIBS) $(GST_CFLAGS) -o gst-encode gst-encode.c \
		gst-encode-test.c 

