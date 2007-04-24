include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = Banshee.Widgets
ASSEMBLY = $(ASSEMBLY_NAME).dll

banshee_widgetsdir = $(pkglibdir)
banshee_widgets_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/ActionButton.cs \
	$(srcdir)/AltProgressBar.cs \
	$(srcdir)/CoverArtView.cs \
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
	$(srcdir)/DiscUsageDisplay.cs \
	$(srcdir)/Tile.cs \
	$(srcdir)/TileView.cs \
	$(srcdir)/RatingEntry.cs \
	$(srcdir)/ShadowContainer.cs \
	$(srcdir)/DictionaryComboBox.cs \
	$(srcdir)/FadingAlignment.cs \
	$(srcdir)/ComplexMenuItem.cs \
	$(srcdir)/RatingMenuItem.cs

Banshee_Widgets_RESOURCES = \
	data/images/rating-rated.png \
	data/images/rating-unrated.png

Banshee_Widgets_EXPANDED_RESOURCES = $(foreach resource,$(Banshee_Widgets_RESOURCES), $(top_srcdir)/$(resource))

Banshee_Widgets_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(Banshee_Widgets_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(Banshee_Widgets_EXPANDED_RESOURCES)
	$(BUILD_LIB) -unsafe -out:$@ $(LINK_GTK) $(LINK_MONO_UNIX) -r:Mono.Cairo $(Banshee_Widgets_EXPANDED_RESOURCES_BUILD) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY).config.in

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb *.dll *.exe *.pidb
MAINTAINERCLEANFILES = Makefile.in

