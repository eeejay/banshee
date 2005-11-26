ASSEMBLY_SOURCES = \
	$(srcdir)/HxPlayer.cs    \
	$(srcdir)/HxUnmanaged.cs \
	$(srcdir)/HxEvents.cs    \
	$(srcdir)/HxDefines.cs   \
	$(srcdir)/HxCallbacks.cs \
	$(srcdir)/HelixPlayer.cs

EXTRA_DIST = \
	$(ASSEMBLY_SOURCES) \
	HxPlayerTest.cs \
	libhxclient.so.1.0 \
	$(ASSEMBLY).config.in

DISTCLEANFILES = Makefile.in
CLEANFILES = *.dll *.mdb *.exe *.pidb

if ENABLE_HELIX

MCS_FLAGS = -debug -nowarn:0169
ASSEMBLY_NAME = helix-sharp
ASSEMBLY = $(ASSEMBLY_NAME).dll

helixenginedir = $(pkglibdir)/Banshee.MediaEngine/Helix
helixengine_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:Mono.Posix $(ASSEMBLY_SOURCES)

install-data-hook:
	$(INSTALL) -m 0755 $(srcdir)/libhxclient.so.1.0 $(DESTDIR)$(pkglibdir)/Banshee.MediaEngine/Helix

uninstall-data:
	rm -f $(DESTDIR)$(pkglibdir)/Banshee.MediaEngine/Helix/libhxclient.so.1.0

test:
	$(MCS) -out:hxplayer.exe HxPlayer.cs HxCallbacks.cs HxUnmanaged.cs HxDefines.cs HxEvents.cs HxPlayerTest.cs

endif

