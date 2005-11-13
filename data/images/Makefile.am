SUBDIRS = ipod

hicolordir = $(datadir)/icons/hicolor

install-data-local:
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/scalable/apps
	$(INSTALL_DATA) $(srcdir)/music-player-banshee.svg $(DESTDIR)$(hicolordir)/scalable/apps/music-player-banshee.svg
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/16x16/apps
	$(INSTALL_DATA) $(srcdir)/music-player-banshee-16.png $(DESTDIR)$(hicolordir)/16x16/apps/music-player-banshee.png
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/22x22/apps
	$(INSTALL_DATA) $(srcdir)/music-player-banshee-22.png $(DESTDIR)$(hicolordir)/22x22/apps/music-player-banshee.png
	$(mkinstalldirs) $(DESTDIR)$(hicolordir)/24x24/apps
	$(INSTALL_DATA) $(srcdir)/music-player-banshee-24.png $(DESTDIR)$(hicolordir)/24x24/apps/music-player-banshee.png

uninstall-hook: 
	rm -f $(DESTDIR)$(hicolordir)/scalable/apps/music-player-banshee.svg
	rm -f $(DESTDIR)$(hicolordir)/16x16/apps/music-player-banshee.png
	rm -f $(DESTDIR)$(hicolordir)/22x22/apps/music-player-banshee.png
	rm -f $(DESTDIR)$(hicolordir)/24x24/apps/music-player-banshee.png

DISTCLEANFILES = Makefile.in

EXTRA_DIST = \
	advanced-icon-32.png \
	banshee-logo.png \
	blue-speaker.png \
	busy-spinner-36.png \
	busy-spinner.png \
	circle.png \
	encoding-icon-32.png \
	icon-album.png \
	icon-artist.png \
	icon-title.png \
	library-icon-32.png \
	media-eject.png \
	media-next.png \
	media-pause.png \
	media-play.png \
	media-prev.png \
	media-repeat.png \
	media-shuffle.png \
	media-stop.png \
	music-player-banshee-16.png \
	music-player-banshee-22.png \
	music-player-banshee-24.png \
	music-player-banshee.png \
	music-player-banshee.svg \
	now-playing-arrow.png \
	playlist-icon-large.png \
	search-entry-icon-hover.png \
	search-entry-icon.png \
	song-drm.png \
	source-cd-audio.png \
	source-library.png \
	source-playlist.png \
	star.png \
	sync-needed.png \
	tray-icon.png \
	volume-decrease.png \
	volume-increase.png \
	volume-max.png \
	volume-med.png \
	volume-min.png \
	volume-zero.png \
	cd-action-rip-16.png \
	cd-action-rip-24.png \
	cd-action-burn-24.png \
	cd-action-burn-32.png \
	cd-action-burn-48.png \
	encode-action-24.png 
