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
	$(srcdir)/NotificationAreaIcon.cs \
	$(srcdir)/HighlightMessageArea.cs \
	$(srcdir)/PropertyTable.cs

ASSEMBLY_RESOURCES = \
	Resources/search-entry-icon.png \
	Resources/search-entry-icon-hover.png

ASSEMBLY_RESOURCES_GENERATED = $(foreach res,$(ASSEMBLY_RESOURCES), $(addprefix -resource:,$(srcdir)/$(res)),$(notdir $(res)))

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) $(GTKSHARP_LIBS) -r:Mono.Posix $(ASSEMBLY_RESOURCES_GENERATED) -target:library -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(ASSEMBLY_RESOURCES) $(ASSEMBLY).config.in

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = *.mdb Makefile.in *.dll *.exe *.pidb

