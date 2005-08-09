ASSEMBLY_SOURCES = \
	$(srcdir)/HxPlayer.cs    \
	$(srcdir)/HxUnmanaged.cs \
	$(srcdir)/HxEvents.cs    \
	$(srcdir)/HxDefines.cs   \
	$(srcdir)/HxCallbackDelegates.cs \
	$(srcdir)/HelixPlayer.cs

EXTRA_DIST = $(ASSEMBLY_SOURCES) HxPlayerTest.cs HXPlayer.h HXCompat.h libhxclient.a libhxclient.so
DISTCLEANFILES = Makefile.in
CLEANFILES = *.dll *.mdb *.exe *.pidb

if ENABLE_HELIX
SUBDIRS = include

MCS_FLAGS = -debug -nowarn:0169
ASSEMBLY_NAME = helix-sharp
ASSEMBLY = $(ASSEMBLY_NAME).dll

INCLUDES = -I$(srcdir)/include -include HXCompat.h

helixenginelibdir = $(pkglibdir)/mediaengines/helix
helixenginelib_LTLIBRARIES = libhxplayer.la
helixenginelib_DATA = libhxclient.a 
helixenginelib_SCRIPTS = $(ASSEMBLY)

libhxplayer_la_SOURCES = \
	HXPlayer.c
	
libhxplayer_la_LIBADD = -L$(srcdir) -lstdc++ -lpthread -ldl -lhxclient

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/plugincore/plugin-core.dll $(GTKSHARP_LIBS) $(ASSEMBLY_SOURCES)

install-data-hook:
	$(INSTALL) -m 0755 $(srcdir)/libhxclient.so $(DESTDIR)$(pkglibdir)/mediaengines/helix

uninstall-data:
	rm -f $(DESTDIR)$(pkglibdir)/mediaengines/helix/libhxclient.so
endif

