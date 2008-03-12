ASSEMBLY = Nereid
TARGET = exe
LINK = $(REF_NEREID)
SOURCES =  \
	Nereid/Client.cs \
	Nereid/PlayerInterface.cs \
	Nereid/ViewContainer.cs

bin_SCRIPTS = banshee-1

include $(top_srcdir)/build/build.mk

run:
	pushd $(top_srcdir)/src/Core; \
	make run; \
	popd
