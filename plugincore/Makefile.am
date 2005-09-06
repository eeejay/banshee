MCS_FLAGS = -debug
ASSEMBLY_NAME = plugin-core
ASSEMBLY = $(ASSEMBLY_NAME).dll

plugincoredir = $(pkglibdir)
plugincore_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES = \
	$(srcdir)/IPlayerEngine.cs \
	$(srcdir)/TrackInfo.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) $(GTKSHARP_LIBS) -r:Mono.Posix -target:library  -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) 

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb Makefile.in *.dll *.exe *.pidb

