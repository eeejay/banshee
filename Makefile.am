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
	intltool-extract.in \
	intltool-merge.in   \
	intltool-update.in  \
	banshee.pc    \
	*.bak         \
	*~            \
	*.pidb

