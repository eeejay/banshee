INCLUDES = $(BANSHEE_CFLAGS)

sonancelibdir = $(pkglibdir)
sonancelib_LTLIBRARIES = libsonance.la

libsonance_la_LDFLAGS = -avoid-version -module
libsonance_la_SOURCES = \
	eggtrayicon.c eggtrayicon.h

libsonance_la_LIBADD = $(BANSHEE_LIBS)

DISTCLEANFILES = Makefile.in

