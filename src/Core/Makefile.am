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
	@rm -rf .run.tmp; \
	mkdir -p .run.tmp/Addins; \
	for dir in $(MONO_BASE_PATH); do \
		cp $$dir/*.{dll,exe,mdb,config} .run.tmp/ 2>/dev/null; \
	done; \
	for dir in $(MONO_ADDINS_PATH); do \
		cp $$dir/*.{dll,mdb,config} .run.tmp/Addins 2>/dev/null; \
	done; \
	cp $(top_builddir)/libbanshee/.libs/libbanshee.so .run.tmp; \
	pushd .run.tmp; \
	echo "<Addins><Directory>./Addins</Directory></Addins>" > Nereid.addins; \
	$(MONO) --debug Nereid.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS); \
	popd;


#	rm -rf .run.tmp

MAINTAINERCLEANFILES = Makefile.in

