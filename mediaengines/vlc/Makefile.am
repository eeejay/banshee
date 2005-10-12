MCS_FLAGS = -debug -nowarn:0169
ASSEMBLY_NAME = vlc-player
ASSEMBLY = $(ASSEMBLY_NAME).dll

vlcenginelibdir = $(pkglibdir)/mediaengines/vlc
vlcenginelib_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config
ASSEMBLY_SOURCES = $(srcdir)/VLC.cs $(srcdir)/VlcPlayerEngine.cs

DISTCLEANFILES = Makefile.in
CLEANFILES = *.exe *.dll *.mdb *.pidb

EXTRA_DIST = $(ASSEMBLY).config.in $(ASSEMBLY_SOURCES)

if ENABLE_VLC
all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ $(GTKSHARP_LIBS) -r:$(top_builddir)/plugincore/plugin-core.dll -r:Mono.Posix $(ASSEMBLY_SOURCES)


install-data-hook:
	if [ -e $(srcdir)/libvlc.so ]; then \
		$(INSTALL) -m 0755 $(srcdir)/libvlc.so $(DESTDIR)$(vlcenginelibdir)/; \
	fi;

uninstall-hook:
	rm -f $(DESTDIR)$(vlcenginelibdir)/libvlc.so;

endif
