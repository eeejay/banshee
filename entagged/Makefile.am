MCS_FLAGS = -debug
ASSEMBLY_NAME = entagged-sharp
ASSEMBLY = $(ASSEMBLY_NAME).dll

entaggeddir = $(pkglibdir)
entagged_SCRIPTS = $(ASSEMBLY)

include entagged-sharp.sources

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) entagged-sharp.sources 

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb Makefile.in *.dll *.exe *.pidb

