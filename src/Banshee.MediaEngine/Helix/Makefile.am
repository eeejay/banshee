ASSEMBLY_SOURCES = \
	$(srcdir)/HelixRemotePlayer.cs \
	$(srcdir)/HelixRemotePlayerEngine.cs
	
EXTRA_DIST = \
	$(ASSEMBLY_SOURCES) \
	$(ASSEMBLY).config.in

DISTCLEANFILES = Makefile.in
CLEANFILES = *.dll *.mdb *.exe *.pidb

if ENABLE_HELIX

MCS_FLAGS = -debug
ASSEMBLY_NAME = HelixRemotePlayerEngine
ASSEMBLY = $(ASSEMBLY_NAME).dll

helixenginedir = $(pkglibdir)/Banshee.MediaEngine/Helix
helixengine_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:Mono.Posix $(DBUSSHARP_LIBS) $(GTKSHARP_LIBS) $(ASSEMBLY_SOURCES)

endif

