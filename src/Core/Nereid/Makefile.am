ASSEMBLY = Nereid
TARGET = exe
LINK = $(REF_NEREID)
SOURCES =  \
	Nereid/Entry.cs \
	Nereid/PlayerInterface.cs \
	Nereid/PlayerWindowSchema.cs \
	Nereid/ViewContainer.cs

bin_SCRIPTS = nereid

include $(top_srcdir)/build/build.mk

run:
	pushd $(top_srcdir)/src/Core; \
	make run; \
	popd
