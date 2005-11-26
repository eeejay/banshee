MCS_FLAGS = -debug -nowarn:0169
ASSEMBLY_NAME = gst-player
ASSEMBLY = $(ASSEMBLY_NAME).dll

gstenginelibdir = $(pkglibdir)/Banshee.MediaEngine/Gstreamer
gstenginelib_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config 

ASSEMBLY_SOURCES = $(srcdir)/GstPlayerEngine.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ $(GTKSHARP_LIBS) -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:Mono.Posix $(ASSEMBLY_SOURCES)

DISTCLEANFILES = Makefile.in
CLEANFILES = *.exe *.dll *.mdb *.pidb

EXTRA_DIST = $(ASSEMBLY).config.in  $(ASSEMBLY_SOURCES)

