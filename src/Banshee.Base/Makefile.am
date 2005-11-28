MCS_FLAGS = -debug 
ASSEMBLY_NAME = Banshee.Base
ASSEMBLY = $(ASSEMBLY_NAME).dll

bansheebasedir = $(pkglibdir)
bansheebase_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

bansheebase_sources_in = ConfigureDefines.cs.in
bansheebase_generated_sources = $(bansheebase_sources_in:.in=)

ASSEMBLY_SOURCES = \
	$(srcdir)/HalCore.cs \
	$(srcdir)/Utilities.cs \
	$(srcdir)/ArgumentQueue.cs \
	$(srcdir)/LogCore.cs \
	$(srcdir)/AmazonCoverFetcher.cs \
	$(srcdir)/Paths.cs \
	$(srcdir)/TrackInfo.cs \
	$(srcdir)/FileNamePattern.cs \
	$(srcdir)/Globals.cs \
	$(srcdir)/GConfKeys.cs \
	$(srcdir)/IPlayerEngine.cs \
	$(srcdir)/PlayerEngineLoader.cs \
	$(srcdir)/SpecialKeys.cs \
	$(srcdir)/AudioCd/AudioCdCore.cs \
	$(srcdir)/AudioCd/AudioCdTrackInfo.cs \
	$(srcdir)/AudioCd/AudioCdDisk.cs \
	$(srcdir)/Dap/Dap.cs \
	$(srcdir)/Dap/DapCore.cs \
	$(srcdir)/Dap/DapTrackInfo.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -r:$(top_builddir)/src/MusicBrainz/MusicBrainz.dll -r:$(top_builddir)/hal-sharp/hal-sharp.dll $(GTKSHARP_LIBS) -r:Mono.Posix -out:$@ $(ASSEMBLY_SOURCES) $(bansheebase_generated_sources)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(bansheebase_sources_in) $(ASSEMBLY).config.in 
CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb Makefile.in 

