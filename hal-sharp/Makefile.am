include $(top_srcdir)/Makefile.include 

ASSEMBLY_NAME = hal-sharp
ASSEMBLY = $(ASSEMBLY_NAME).dll

haldir = $(pkglibdir)
hal_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

include hal-sharp.sources

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB) -out:$@ $(LINK_MONO_UNIX) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) hal-sharp.sources $(ASSEMBLY).config.in

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb *.dll *.exe
MAINTAINERCLEANFILES = Makefile.in

