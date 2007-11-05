include $(top_srcdir)/build/Common.Makefile

SUBDIRS = \
	Hyena \
	Hyena.Gui \
	Banshee.Core \
	Banshee.Services \
	Banshee.Widgets \
	Banshee.ThickClient \
	Nereid

#Banshee

run:
	MONO_PATH=$(RUN_PATH) mono --debug Nereid/Nereid.exe 

MAINTAINERCLEANFILES = \
	Makefile.in
