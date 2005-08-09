MCS_FLAGS = -debug
ASSEMBLY_NAME = plugin-core
ASSEMBLY = $(ASSEMBLY_NAME).dll

plugincoredir = $(pkglibdir)
plugincore_SCRIPTS = $(ASSEMBLY)

ASSEMBLY_SOURCES = \
	$(srcdir)/IPlayerEngine.cs \
	$(srcdir)/ITrackInfo.cs
	
all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library  -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) 

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb Makefile.in *.dll *.exe *.pidb

