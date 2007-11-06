include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = MusicBrainz
ASSEMBLY = $(ASSEMBLY_NAME).dll

musicbrainzdir = $(pkglibdir)
musicbrainz_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY_SOURCES =  \
	Client.cs \
	ClientVersion.cs \
	Rdf.cs \
	SimpleAlbum.cs \
	SimpleArtist.cs \
	SimpleDisc.cs \
	SimpleQuery.cs \
	SimpleTrack.cs \
	Utilities.cs

if ENABLE_TESTS
all: $(ASSEMBLY) test
else
all: $(ASSEMBLY)
endif

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB) -out:$@ $(ASSEMBLY_SOURCES)

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
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in

