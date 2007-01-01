include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = Banshee.MediaEngine.GStreamer
ASSEMBLY = $(ASSEMBLY_NAME).dll

gstenginelibdir = $(pkglibdir)/Banshee.MediaEngine
gstenginelib_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb 

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/GstPlayerEngine.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_GTK) $(LINK_MONO_UNIX) $(ASSEMBLY_SOURCES)

MAINTAINERCLEANFILES = Makefile.in
CLEANFILES = *.exe *.dll *.mdb *.pidb

EXTRA_DIST = $(ASSEMBLY_SOURCES)

