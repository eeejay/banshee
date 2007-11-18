SUBDIRS = \
	Extras \
	Core \
	Backends \
	Extensions


#Dap \
#	Plugins

EXTRA_DIST = AssemblyInfo.cs.in

run:
	pushd $(top_builddir)/src/Core; \
	make run; \
	popd;

MAINTAINERCLEANFILES = \
	Makefile.in

CLEANFILES = *.mdb *.pidb *.exe *.dll
