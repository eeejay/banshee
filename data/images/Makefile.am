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

MAINTAINERCLEANFILES = Makefile.in

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
	media-skip-forward.png \
	media-playback-pause.png \
	media-playback-start.png \
	media-skip-backward.png \
	media-repeat-all.png \
	media-repeat-none.png \
	media-repeat-single.png \
	media-playlist-shuffle.png \
	media-playlist-continuous.png \
	music-player-banshee-16.png \
	music-player-banshee-22.png \
	music-player-banshee-24.png \
	music-player-banshee.png \
	music-player-banshee.svg \
	now-playing-arrow.png \
	playlist-icon-large.png \
	song-drm.png \
	source-cd-audio.png \
	source-library.png \
	source-playlist.png \
	source-localqueue.png \
	star.png \
	sync-needed.png \
	tray-icon.png \
	audio-volume-decrease.png \
	audio-volume-increase.png \
	audio-volume-high.png \
	audio-volume-medium.png \
	audio-volume-low.png \
	audio-volume-muted.png \
	cd-action-rip-16.png \
	cd-action-rip-24.png \
	cd-action-burn-24.png \
	cd-action-burn-32.png \
	cd-action-burn-48.png \
	encode-action-24.png 

generate-resource-list:
	for i in $(EXTRA_DIST); do \
		echo "data/images/$$i \\"; \
	done;

