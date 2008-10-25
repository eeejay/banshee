SUBDIRS = \
	build \
	data \
	libbanshee \
	gstreamer \
	src \
	docs \
	tests \
	extras \
	po

# Important targets

clean-local:
	rm -rf $(top_builddir)/bin

uninstall-hook:
	if test `find $(pkglibdir) | wc -l` -eq 1; then \
		rmdir $(pkglibdir); \
	fi

# Developer targets

LC_BUILD = $(top_builddir)/bin/share/locale
PO_FILES = $(addprefix po/, $(addsuffix .gmo, $(ALL_LINGUAS)))
MO_FILES = $(patsubst po/%.gmo, $(LC_BUILD)/%/LC_MESSAGES/$(PACKAGE).mo, $(PO_FILES))

$(MO_FILES): $(LC_BUILD)/%/LC_MESSAGES/$(PACKAGE).mo: po/%.gmo
	$(mkdir_p) $(dir $@)
	cp -af '$<' '$@'

run: $(MO_FILES)
	@pushd bin; \
	$(MONO) --debug $(BANSHEE_DEV_MONO_OPTIONS) Nereid.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS); \
	popd;

gdb:
	@pushd bin; \
	gdb mono --eval-command="handle SIGXCPU SIG35 SIGPWR nostop noprint" --eval-command="b g_return_if_fail_warning" --eval-command="r --debug Nereid.exe --debug --uninstalled"; \
	popd;

mdb:
	@pushd bin; \
	mdb -run -args Nereid.exe --debug --uninstalled"; \
	popd;

test:
	@pushd tests; \
	make test \
	popd;

hg:
	pushd bin; \
	echo "class Entry { static void Main () { Hyena.Gui.TestModuleRunner.Run (); } }" > tester.cs; \
	gmcs -r:Hyena.Gui.dll tester.cs; \
	mono --debug tester.exe; \
	rm tester.*; \
	popd; 

EXTRA_DIST = \
	intltool-extract.in \
	intltool-merge.in \
	intltool-update.in \
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
