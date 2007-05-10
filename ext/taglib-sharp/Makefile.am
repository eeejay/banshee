include $(top_srcdir)/build/Common.Makefile 

ASSEMBLY_NAME = TagLib
ASSEMBLY = $(ASSEMBLY_NAME).dll

taglibdir = $(pkglibdir)
taglib_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

include $(srcdir)/TagLib/TagLib.sources

all: $(ASSEMBLY)

$(ASSEMBLY): $(TAGLIB_CSFILES)
	$(BUILD_LIB) -out:$@ $(LINK_MONO_UNIX) $(TAGLIB_CSFILES)

EXTRA_DIST = $(TAGLIB_CSFILES) $(srcdir)/TagLib/TagLib.sources
CLEANFILES = $(taglib_SCRIPTS)
MAINTAINERCLEANFILES = Makefile.in

