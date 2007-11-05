SUBDIRS = \
	Hyena \
	Hyena.Gui \
	Banshee.Core \
	Banshee.Services \
	Banshee.Widgets \
	Banshee.ThickClient \
	Nereid

include $(top_srcdir)/build/build.environment.mk

run:
	MONO_PATH=$(RUN_PATH) $(MONO) --debug Nereid/Nereid.exe

