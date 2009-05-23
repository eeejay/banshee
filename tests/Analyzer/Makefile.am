include $(top_srcdir)/build/build.environment.mk

ANALYZER_EXE = Analyzer.exe

ALL_TARGETS = $(ANALYZER_EXE)

$(ANALYZER_EXE): Analyzer.cs
	$(MCS) -r:System.Xml.Linq -r:$(DIR_BIN)/Hyena.dll -debug+ -out:$@ $<

all: $(ALL_TARGETS)

EXTRA_DIST = Analyzer.cs

CLEANFILES = *.exe *.mdb
MAINTAINERCLEANFILES = Makefile.in
