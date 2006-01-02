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
	$(srcdir)/SimpleArtist.cs \
	$(srcdir)/SimpleAlbum.cs \
	$(srcdir)/SimpleTrack.cs \
	$(srcdir)/SimpleQuery.cs \
	$(srcdir)/Utilities.cs

if ENABLE_TESTS
all: $(ASSEMBLY) test
else
all: $(ASSEMBLY)
endif

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library  -out:$@ $(ASSEMBLY_SOURCES)

test: simple-disc-test.exe simple-lookup-test.exe

simple-disc-test.exe: SimpleDiscTest.cs 
	$(MCS) $(MCS_FLAGS) -out:$@ -r:$(ASSEMBLY) $<

simple-lookup-test.exe: SimpleLookupTest.cs
	$(MCS) $(MCS_FLAGS) -out:$@ -r:$(ASSEMBLY) $<

EXTRA_DIST = \
	$(ASSEMBLY_SOURCES) \
	$(ASSEMBLY).config.in \
	SimpleDiscTest.cs \
	SimpleLookupTest.cs 

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb Makefile.in

