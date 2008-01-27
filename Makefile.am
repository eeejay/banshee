SUBDIRS = \
	build \
	data \
	libbanshee \
	src \
	docs \
	tests \
	extras \
	po

EXTRA_DIST = \
	intltool-extract.in \
	intltool-merge.in \
	intltool-update.in \
	mkinstalldirs \
	AUTHORS \
	HACKING

CLEANFILES = \
	intltool-extract \
	intltool-update \
	intltool-merge

DISTCLEANFILES = \
	*.bak \
	*~ \
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
	pushd $(top_builddir)/src/Core; \
	make run; \
	popd;

uninstall-hook:
	if test `find $(pkglibdir) | wc -l` -eq 1; then \
		rmdir $(pkglibdir); \
	fi

