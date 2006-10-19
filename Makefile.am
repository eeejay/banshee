SUBDIRS =          \
	build          \
	data           \
	libbanshee     \
	taglib-sharp   \
	dbus-sharp     \
	hal-sharp      \
	src            \
	docs           \
	tests          \
	tools          \
	branding       \
	po

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = banshee.pc

EXTRA_DIST =            \
	banshee.pc.in       \
	intltool-extract.in \
	intltool-merge.in   \
	intltool-update.in  \
	build/Common.Makefile    \
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

run:
	pushd $(top_builddir)/src; \
	make run; \
	popd;

