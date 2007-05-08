include $(top_srcdir)/build/Common.Makefile 

ASSEMBLY_NAME = TagLib
ASSEMBLY = $(ASSEMBLY_NAME).dll

taglibdir = $(pkglibdir)
taglib_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

include $(srcdir)/TagLib.sources

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB) -out:$@ $(LINK_MONO_UNIX) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(srcdir)/TagLib.sources
CLEANFILES = $(taglib_SCRIPTS)
MAINTAINERCLEANFILES = Makefile.in

