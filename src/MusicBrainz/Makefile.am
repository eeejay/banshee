MCS_FLAGS = -debug
ASSEMBLY_NAME = MusicBrainz
ASSEMBLY = $(ASSEMBLY_NAME).dll

musicbrainzdir = $(pkglibdir)
musicbrainz_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY_SOURCES = \
	Client.cs \
	ClientVersion.cs \
	Rdf.cs \
	SimpleDisc.cs \
	SimpleTrack.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library  -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in SimpleDiscTest.cs

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb Makefile.in *.dll *.exe

