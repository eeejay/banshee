MCS_FLAGS = -debug
ASSEMBLY_NAME = hal-sharp
ASSEMBLY = $(ASSEMBLY_NAME).dll

haldir = $(pkglibdir)
hal_SCRIPTS = $(ASSEMBLY)

include hal-sharp.sources

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -r:Mono.Posix -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) hal-sharp.sources

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb Makefile.in *.dll *.exe

