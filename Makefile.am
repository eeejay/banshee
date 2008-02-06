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
	@pushd bin; \
	$(MONO) --debug Nereid.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS); \
	popd;

clean-local:
	rm -rf $(top_builddir)/bin

uninstall-hook:
	if test `find $(pkglibdir) | wc -l` -eq 1; then \
		rmdir $(pkglibdir); \
	fi

dist-windows: all
	rm -rf banshee-windows; \
	mkdir banshee-windows; \
	cp -rf bin/*.{dll,exe} banshee-windows; \
	zip banshee-windows.zip -r banshee-windows; \
	rm -rf banshee-windows

