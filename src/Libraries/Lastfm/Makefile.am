ASSEMBLY = Lastfm
TARGET = library
LINK = $(REF_LASTFM)

SOURCES = \
	Lastfm/Account.cs \
	Lastfm/Browser.cs \
	Lastfm/RadioConnection.cs \
	Lastfm.Data/DataCore.cs \
	Lastfm.Data/DataEntry.cs \
	Lastfm.Data/DataEntryCollection.cs \
	Lastfm.Data/LastfmData.cs \
	Lastfm.Data/LastfmArtistData.cs \
	Lastfm.Data/LastfmUserData.cs 

include $(top_srcdir)/build/build.mk

test: Test.cs
	gmcs -r:$(top_builddir)/bin/Lastfm.dll -out:TestLastfm.exe Test.cs && \
	mv TestLastfm.exe $(top_builddir)/bin/
	@pushd $(top_builddir)/bin/; mono --debug TestLastfm.exe; \
	popd;
