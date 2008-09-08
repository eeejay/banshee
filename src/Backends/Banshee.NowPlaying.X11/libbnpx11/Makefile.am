SOURCES = bacon-resize.c bacon-resize.h

if HAVE_XVIDMODE
bnpx11dir = $(pkglibdir)/Backends
bnpx11_LTLIBRARIES = libbnpx11.la

libbnpx11_la_SOURCES = $(SOURCES)
libbnpx11_la_CFLAGS = $(BNPX_CFLAGS) $(BNPX_GTK_CFLAGS)
libbnpx11_la_LIBADD = $(BNPX_LIBS) $(BNPX_GTK_LIBS) -lX11 -lXxf86vm -lXrandr
libbnpx11_la_LDFLAGS = -avoid-version -module

noinst_HEADERS = bacon-resize.h

all: $(top_builddir)/bin/libbnpx11.so

$(top_builddir)/bin/libbnpx11.so: libbnpx11.la
	mkdir -p $(top_builddir)/bin
	cp -f .libs/libbnpx11.so $@

CLEANFILES = $(top_builddir)/bin/libbnpx11.so

else
EXTRA_DIST = $(SOURCES)
endif

MAINTAINERCLEANFILES = Makefile.in

