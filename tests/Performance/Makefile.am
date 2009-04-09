include $(top_srcdir)/build/build.environment.mk

PERFORMANCE_ASSEMBLY = Performance.dll
PERFORMANCE_EXE = Performance.exe

ALL_TARGETS = $(PERFORMANCE_ASSEMBLY) $(PERFORMANCE_EXE)

$(PERFORMANCE_ASSEMBLY): PerformanceTests.cs
	$(MCS) -target:library -r:$(DIR_BIN)/Hyena.dll $(NUNIT_LIBS) $(LINK_BANSHEE_THICKCLIENT_DEPS) -out:$@ $<

$(PERFORMANCE_EXE): PerformanceTests.cs
	$(MCS) -target:exe -r:$(DIR_BIN)/Hyena.dll $(NUNIT_LIBS) $(LINK_BANSHEE_THICKCLIENT_DEPS) -out:$@ $<

all: $(ALL_TARGETS)

CLEANFILES = *.dll *.exe *.mdb
MAINTAINERCLEANFILES = Makefile.in
