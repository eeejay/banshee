INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(HAL_CFLAGS)

bansheelibdir = $(pkglibdir)
bansheelib_LTLIBRARIES = libbanshee.la

libbanshee_la_LDFLAGS = -avoid-version -module
libbanshee_la_SOURCES = \
	cd-detect.c \
	cd-detect.h

libbanshee_la_LIBADD = $(HAL_LIBS)

CLEANFILES = cdtest
DISTCLEANFILES = Makefile.in

EXTRA_DIST = cd-test.c

test:
	$(CC) -o cdtest $(HAL_LIBS) $(HAL_CFLAGS) cd-test.c $(libbanshee_la_SOURCES)

