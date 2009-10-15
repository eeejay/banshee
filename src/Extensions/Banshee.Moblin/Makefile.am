ASSEMBLY = Banshee.Moblin
TARGET = library
LINK = $(REF_EXTENSION_MOBLIN) $(LINK_EXTENSION_PLAYQUEUE_DEPS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Moblin/MediaPanelContents.cs \
	Banshee.Moblin/MoblinPanel.cs \
	Banshee.Moblin/MoblinService.cs \
	Banshee.Moblin/MoblinTrackInfoDisplay.cs \
	Banshee.Moblin/PlaybackBox.cs \
	Banshee.Moblin/PlayQueueBox.cs \
	Banshee.Moblin/RecentAlbumsList.cs \
	Banshee.Moblin/RecentAlbumsView.cs \
	Banshee.Moblin/SearchEntry.cs \
	Banshee.Moblin/SearchHeader.cs \
	Mutter/PanelClient.cs \
	Mutter/PanelGtk.cs \
	Mutter/SetSizeHandler.cs

RESOURCES = Banshee.Moblin.addin.xml

if HAVE_MOBLIN

include $(top_srcdir)/build/build.mk

module_SCRIPTS += Banshee.Moblin.dll.config

paneldir = $(libdir)/mutter-moblin
panel_SCRIPTS = moblin-panel-media

servicedir = $(datadir)/dbus-1/services
service_DATA = org.moblin.UX.Shell.Panels.media.service

org.moblin.UX.Shell.Panels.media.service: org.moblin.UX.Shell.Panels.media.service.in $(top_builddir)/config.log
	sed -e "s|\@libexecdir\@|$(libdir)/mutter-moblin|" $< > $@

clean-local:
	rm org.moblin.UX.Shell.Panels.media.service

else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

EXTRA_DIST += \
	Banshee.Moblin.dll.config \
	moblin-panel-media \
	org.moblin.UX.Shell.Panels.media.service.in
