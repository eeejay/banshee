MCS_FLAGS = -debug
ASSEMBLY_NAME = MusicBrainz
ASSEMBLY = $(ASSEMBLY_NAME).dll

musicbrainzdir = $(pkglibdir)
musicbrainz_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY_SOURCES = \
	$(srcdir)/Client.cs \
	$(srcdir)/ClientVersion.cs \
	$(srcdir)/Rdf.cs \
	$(srcdir)/SimpleDisc.cs \
	$(srcdir)/SimpleTrack.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library  -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in SimpleDiscTest.cs

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb Makefile.in *.dll *.exe

