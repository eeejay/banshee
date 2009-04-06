include $(top_srcdir)/build/build.environment.mk

PERFORMANCE_ASSEMBLY = Performance.dll

ALL_TARGETS = $(PERFORMANCE_ASSEMBLY)

$(PERFORMANCE_ASSEMBLY): PerformanceTests.cs
	$(MCS) -target:library -r:$(DIR_BIN)/Hyena.dll $(NUNIT_LIBS) $(LINK_BANSHEE_THICKCLIENT_DEPS) -out:$@ $<

all: $(ALL_TARGETS)

CLEANFILES = *.dll *.mdb
MAINTAINERCLEANFILES = Makefile.in
