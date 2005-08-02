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
	
libhxplayer_la_LIBADD = -L. -lstdc++ -lpthread -ldl -lhxclient

ASSEMBLY_SOURCES = \
	HxPlayer.cs    \
	HxUnmanaged.cs \
	HxEvents.cs    \
	HxDefines.cs   \
	HxCallbackDelegates.cs \
	HelixPlayer.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/plugincore/plugin-core.dll $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) HxPlayerTest.cs HXPlayer.h HXCompat.h libhxclient.a libhxclient.so
DISTCLEANFILES = Makefile.in
CLEANFILES = *.dll *.mdb *.exe

install-data-hook:
	$(INSTALL) -c -c -m 0755 libhxclient.so $(pkglibdir)/mediaengines/helix

uninstall-data:
	rm -f $(pkglibdir)/mediaengines/helix/libhxclient.so

