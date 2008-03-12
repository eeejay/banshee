theme = hicolor
themedir = $(datadir)/icons/$(theme)
theme_icons = \
	apps,media-player-banshee-16.png \
	apps,media-player-banshee-22.png \
	apps,media-player-banshee-24.png \
	apps,media-player-banshee-32.png \
	apps,media-player-banshee-48.png \
	apps,media-player-banshee-192.png

install_icon_exec = $(top_srcdir)/build/icon-theme-installer \
	-t "$(theme)" \
	-s "$(srcdir)" \
	-d "x$(DESTDIR)" \
	-b "$(themedir)" \
	-m "$(mkinstalldirs)" \
	-x "$(INSTALL_DATA)"

install-data-local:
	@-$(install_icon_exec) -i $(theme_icons)

uninstall-hook: 
	@-$(install_icon_exec) -u $(theme_icons)
	
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(wildcard *.png *.svg) $(wildcard src/*.svg)

