ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/HelixRemotePlayer.cs \
	$(srcdir)/HelixRemotePlayerEngine.cs

if ENABLE_HELIX
	
EXTRA_DIST = \
	$(ASSEMBLY_SOURCES) \
	$(ASSEMBLY).config.in

MAINTAINERCLEANFILES = Makefile.in
CLEANFILES = *.dll *.mdb *.exe *.pidb

MCS_FLAGS = -debug
ASSEMBLY_NAME = HelixRemotePlayerEngine
ASSEMBLY = $(ASSEMBLY_NAME).dll

helixenginedir = $(pkglibdir)/Banshee.MediaEngine
helixengine_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:Mono.Posix $(DBUSSHARP_LIBS) $(GTKSHARP_LIBS) $(ASSEMBLY_SOURCES)

endif

