SUBDIRS = \
	Extras \
	Core \
	Engines \
	Dap \
	Plugins

EXTRA_DIST = AssemblyInfo.cs.in

run:
	pushd $(top_builddir)/src/Core/Banshee; \
	make run; \
	popd;

MAINTAINERCLEANFILES = \
	Makefile.in
