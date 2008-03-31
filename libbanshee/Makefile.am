INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(LIBBANSHEE_CFLAGS) \
	$(GST_CFLAGS) \
	$(LIBNAUTILUS_BURN_CFLAGS)

bansheelibdir = $(pkglibdir)
bansheelib_LTLIBRARIES = libbanshee.la

libbanshee_la_LDFLAGS = -avoid-version -module
libbanshee_la_SOURCES =  \
	banshee-player.c \
	gst-cd-rip-0.10.c \
	gst-misc-0.10.c \
	gst-playback.h \
	gst-tagger.c \
	gst-transcode-0.10.c

OLD_CODE = \
	gst-mbtrm.h \
	gst-mbtrm.c \
	nautilus-burn.c \
	misc-glue.c

libbanshee_la_LIBADD = \
	$(LIBBANSHEE_LIBS) \
	$(GST_LIBS) \
	$(LIBNAUTILUS_BURN_LIBS)

all: $(top_builddir)/bin/libbanshee.so

$(top_builddir)/bin/libbanshee.so: .libs/libbanshee.so	
	mkdir -p $(top_builddir)/bin
	cp -f $< $@

CLEANFILES = $(top_builddir)/bin/libbanshee.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(libbanshee_la_SOURCES)

