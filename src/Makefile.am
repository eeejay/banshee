SUBDIRS = \
	Extras \
	Core \
	Backends


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
