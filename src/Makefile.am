SUBDIRS = \
	Extras \
	Core \
	Engines


#Dap \
#	Plugins

EXTRA_DIST = AssemblyInfo.cs.in

run:
	pushd $(top_builddir)/src/Core; \
	make run; \
	popd;

run-nocheck:
	pushd $(top_builddir)/src/Core/Banshee; \
	make run-nocheck; \
	popd;

MAINTAINERCLEANFILES = \
	Makefile.in

CLEANFILES = *.mdb *.pidb *.exe *.dll
