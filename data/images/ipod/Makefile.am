IPOD_ICONS = \
	portable-media-ipod-mini-blue \
	portable-media-ipod-mini-gold \
	portable-media-ipod-mini-green \
	portable-media-ipod-mini-pink \
	portable-media-ipod-mini-silver \
	portable-media-ipod-nano-black \
	portable-media-ipod-nano-white \
	portable-media-ipod-shuffle \
	portable-media-ipod-standard-color \
	portable-media-ipod-standard-monochrome \
	portable-media-ipod-U2-color \
	portable-media-ipod-U2-monochrome \
	portable-media-motorola-rokr

SVG_IPOD_ICONS = $(foreach icon, $(IPOD_ICONS), $(srcdir)/scalable/$(icon).svg)
RASTER_16_IPOD_ICONS = $(foreach icon, $(IPOD_ICONS), $(srcdir)/16x16/$(icon).png)
RASTER_22_IPOD_ICONS = $(foreach icon, $(IPOD_ICONS), $(srcdir)/22x22/$(icon).png)
RASTER_48_IPOD_ICONS = $(foreach icon, $(IPOD_ICONS), $(srcdir)/48x48/$(icon).png)
RASTER_128_IPOD_ICONS = $(foreach icon, $(IPOD_ICONS), $(srcdir)/128x128/$(icon).png)

EXTRA_DIST = $(SVG_IPOD_ICONS) $(RASTER_22_IPOD_ICONS) $(RASTER_22_IPOD_ICONS) $(RASTER_48_IPOD_ICONS) $(RASTER_128_IPOD_ICONS)

DISTCLEANFILES = Makefile.in

hicolordir = $(datadir)/icons/hicolor

install-data-local:
	dir_scalable=$(DESTDIR)$(hicolordir)/scalable/extras/devices; \
	dir_16=$(DESTDIR)$(hicolordir)/16x16/extras/devices; \
	dir_22=$(DESTDIR)$(hicolordir)/22x22/extras/devices; \
	dir_48=$(DESTDIR)$(hicolordir)/48x48/extras/devices; \
	dir_128=$(DESTDIR)$(hicolordir)/128x128/extras/devices; \
	$(mkinstalldirs) $$dir_scalable $$dir_16 $$dir_22 $$dir_48 $$dir_128; \
	for i in $(SVG_IPOD_ICONS); do $(INSTALL_DATA) $$i $$dir_scalable; done; \
	for i in $(RASTER_16_IPOD_ICONS); do $(INSTALL_DATA) $$i $$dir_16; done; \
	for i in $(RASTER_22_IPOD_ICONS); do $(INSTALL_DATA) $$i $$dir_22; done; \
	for i in $(RASTER_48_IPOD_ICONS); do $(INSTALL_DATA) $$i $$dir_48; done; \
	for i in $(RASTER_128_IPOD_ICONS); do $(INSTALL_DATA) $$i $$dir_128; done; 

uninstall-hook:
	for i in $(SVG_IPOD_ICONS); do rm -f $(DESTDIR)$(hicolordir)/scalable/extras/devices/`basename $$i`; done; \
	for i in $(RASTER_16_IPOD_ICONS); do rm -f $(DESTDIR)$(hicolordir)/16x16/extras/devices/`basename $$i`; done; \
	for i in $(RASTER_22_IPOD_ICONS); do rm -f $(DESTDIR)$(hicolordir)/22x22/extras/devices/`basename $$i`; done; \
	for i in $(RASTER_48_IPOD_ICONS); do rm -f $(DESTDIR)$(hicolordir)/48x48/extras/devices/`basename $$i`; done; \
	for i in $(RASTER_128_IPOD_ICONS); do rm -f $(DESTDIR)$(hicolordir)/128x128/extras/devices/`basename $$i`; done;

generate:
	for s in 48 128; do \
		SIZE_DIR="$(srcdir)/$${s}x$${s}"; \
		if [ ! -d $$SIZE_DIR ]; then \
			mkdir $$SIZE_DIR; \
		fi; \
		for i in $(SVG_IPOD_ICONS); do \
			rsvg -w $$s -h $$s -f png $$i $$SIZE_DIR/`basename $$i .svg`.png; \
		done; \
	done;
