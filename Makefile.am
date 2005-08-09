SUBDIRS = \
	data \
	libsonance \
	plugincore \
	mediaengines \
	burn-sharp \
	entagged \
	hal-sharp \
	src 

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = banshee.pc

EXTRA_DIST = \
	banshee.pc.in

CLEANFILES=banshee.pc
DISTCLEANFILES=config.guess config.sub configure install-sh missing depcomp Makefile.in *.bak *~ *.pidb mkinstalldirs ltmain.sh aclocal.m4 config.h.in INSTALL

uninstall-hook:
	rmdir $(pkglibdir)

