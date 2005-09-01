SUBDIRS = xing

INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(HAL_CFLAGS) \
	$(GST_CFLAGS) \
	-I$(top_builddir)/libbanshee/xing

bansheelibdir = $(pkglibdir)
bansheelib_LTLIBRARIES = libbanshee.la

libbanshee_la_LDFLAGS = -avoid-version -module
libbanshee_la_SOURCES = \
	cd-detect.c \
	cd-detect.h \
	cd-info.c   \
	cd-info.h   \
	cd-rip.c    \
	cd-rip.h    \
	gst-init.c  \
	gst-init.h

libbanshee_la_LIBADD = $(HAL_LIBS) $(GST_LIBS) $(top_builddir)/libbanshee/xing/libgstxingenc.la

CLEANFILES = cdtest
DISTCLEANFILES = Makefile.in

EXTRA_DIST = cd-test.c

test:
	$(CC) -o cdtest $(INCLUDES) $(GST_LIBS) $(HAL_LIBS) cd-test.c cd-detect.c cd-info.c cd-rip.c

