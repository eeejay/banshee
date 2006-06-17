MCS_FLAGS = -debug
ASSEMBLY_NAME = Banshee.Widgets
ASSEMBLY = $(ASSEMBLY_NAME).dll

banshee_widgetsdir = $(pkglibdir)
banshee_widgets_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY_SOURCES = \
	$(srcdir)/ActionButton.cs \
	$(srcdir)/AltProgressBar.cs \
	$(srcdir)/CoverArtThumbnail.cs \
	$(srcdir)/DateButton.cs \
	$(srcdir)/EllipsizeLabel.cs \
	$(srcdir)/ImageAnimation.cs \
	$(srcdir)/MultiStateToggleButton.cs \
	$(srcdir)/SearchEntry.cs \
	$(srcdir)/SimpleNotebook.cs \
	$(srcdir)/ToggleState.cs \
	$(srcdir)/VolumeButton.cs \
	$(srcdir)/ActiveUserEvent.cs \
	$(srcdir)/ActiveUserEventsManager.cs \
	$(srcdir)/HigMessageDialog.cs \
	$(srcdir)/HighlightMessageArea.cs \
	$(srcdir)/PropertyTable.cs \
	$(srcdir)/RadialProgress.cs \
	$(srcdir)/LinearProgress.cs \
	$(srcdir)/MessagePane.cs \
	$(srcdir)/LinkLabel.cs \
	$(srcdir)/DrawingUtilities.cs \
	$(srcdir)/SeekSlider.cs \
	$(srcdir)/StreamPositionLabel.cs \
	$(srcdir)/DiscUsageDisplay.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) $(GTKSHARP_LIBS) -r:Mono.Posix -r:Mono.Cairo -target:library -unsafe -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb *.dll *.exe *.pidb
MAINTAINERCLEANFILES = Makefile.in
