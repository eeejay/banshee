SUBDIRS = \
	build \
	data \
	libbanshee \
	src \
	docs \
	tests \
	extras \
	po

WINDOWS_ASSEMBLIES = \
	Hyena.dll \
	Hyena.Gui.dll \
	Banshee.Core.dll \
	Banshee.Services.dll \
	Banshee.ThickClient.dll \
	Banshee.Widgets.dll \
	Banshee.PlayQueue.dll \
	Banshee.Lastfm.dll \
	Banshee.NotificationArea.dll \
	Lastfm.Gui.dll \
	Lastfm.dll \
	Mono.Media.dll \
	Nereid.exe

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
	$(MONO) --debug $(BANSHEE_DEV_MONO_OPTIONS) Nereid.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS); \
	popd;

test:
	@pushd tests; \
	make test \
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
	mkdir banshee-windows/icons; \
	cp -rf win-deps/* banshee-windows; \
	for asm in $(WINDOWS_ASSEMBLIES); do \
		cp -rf bin/$$asm banshee-windows; \
	done; \
	zip banshee-windows.zip -r banshee-windows; \
	rm -rf banshee-windows

