SUBDIRS =          \
	data           \
	libbanshee     \
	burn-sharp     \
	entagged-sharp \
	hal-sharp      \
	src            \
	docs           \
	po

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = banshee.pc

EXTRA_DIST =            \
	banshee.pc.in       \
	intltool-extract.in \
	intltool-merge.in   \
	intltool-update.in  \
	MAINTAINERS         \
	HACKING

CLEANFILES = \
	intltool-extract    \
	intltool-update     \
	intltool-merge

DISTCLEANFILES =        \
	banshee.pc    \
	*.bak         \
	*~            \
	*.pidb

MAINTAINERCLEANFILES = \
	compile \
	INSTALL \
	config.h.in \
	aclocal.m4 \
	ltmain.sh \
	Makefile.in \
	depcomp \
	missing \
	install-sh \
	configure \
	config.sub \
	config.guess \
	intltool-extract.in \
	intltool-merge.in   \
	intltool-update.in  \
	mkinstalldirs

