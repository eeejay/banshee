SUBDIRS = \
	Core \
	Backends \
	Extensions

EXTRA_DIST = AssemblyInfo.cs.in

run:
	pushd $(top_builddir)/src/Core; \
	make run; \
	popd;

MAINTAINERCLEANFILES = \
	Makefile.in

CLEANFILES = *.mdb *.pidb *.exe *.dll
