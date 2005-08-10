ASSEMBLY_SOURCES = \
	$(srcdir)/HxPlayer.cs    \
	$(srcdir)/HxUnmanaged.cs \
	$(srcdir)/HxEvents.cs    \
	$(srcdir)/HxDefines.cs   \
	$(srcdir)/HxCallbackDelegates.cs \
	$(srcdir)/HelixPlayer.cs

EXTRA_DIST = $(ASSEMBLY_SOURCES) HxPlayerTest.cs libhxclient.a libhxclient.so
DISTCLEANFILES = Makefile.in
CLEANFILES = *.dll *.mdb *.exe *.pidb

if ENABLE_HELIX

MCS_FLAGS = -debug -nowarn:0169
ASSEMBLY_NAME = helix-sharp
ASSEMBLY = $(ASSEMBLY_NAME).dll

INCLUDES = -I$(srcdir)/include -include HXCompat.h

helixenginedir = $(pkglibdir)/mediaengines/helix
helixengine_DATA = libhxclient.a 
helixengine_SCRIPTS = $(ASSEMBLY)

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/plugincore/plugin-core.dll $(GTKSHARP_LIBS) $(ASSEMBLY_SOURCES)

install-data-hook:
	$(INSTALL) -m 0755 $(srcdir)/libhxclient.so $(DESTDIR)$(pkglibdir)/mediaengines/helix

uninstall-data:
	rm -f $(DESTDIR)$(pkglibdir)/mediaengines/helix/libhxclient.so
endif

