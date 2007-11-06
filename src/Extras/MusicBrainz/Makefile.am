ASSEMBLY = MusicBrainz
TARGET = library
LINK = $(REF_MUSICBRAINZ)
SOURCES = \
	MusicBrainz/Client.cs \
	MusicBrainz/ClientVersion.cs \
	MusicBrainz/Rdf.cs \
	MusicBrainz/SimpleAlbum.cs \
	MusicBrainz/SimpleArtist.cs \
	MusicBrainz/SimpleDisc.cs \
	MusicBrainz/SimpleQuery.cs \
	MusicBrainz/SimpleTrack.cs \
	MusicBrainz/Utilities.cs

include $(top_srcdir)/build/build.mk

module_SCRIPTS += $(ASSEMBLY_FILE).config
EXTRA_DIST += $(ASSEMBLY_FILE).config.in SimpleDiscTest.cs SimpleLookupTest.cs

if ENABLE_TESTS
all: test
endif

test: simple-disc-test.exe simple-lookup-test.exe

simple-disc-test.exe: SimpleDiscTest.cs 
	$(BUILD) -out:$@ $(LINK_MUSICBRAINZ_DEPS) $<

simple-lookup-test.exe: SimpleLookupTest.cs
	$(BUILD) -out:$@ $(LINK_MUSICBRAINZ_DEPS) $<

