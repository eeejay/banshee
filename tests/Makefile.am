include $(top_srcdir)/build/build.environment.mk

MCS_FLAGS = -debug
NUNIT_FLAGS = @MONO_NUNIT_LIBS@

ASSEMBLY_NAME = banshee-tests
ASSEMBLY = $(ASSEMBLY_NAME).dll
ASSEMBLY_CSFILES =  \
	Banshee.Core/FileNamePatternTests.cs \
	Banshee.Core/KernelTests.cs \
	Banshee.Services/PlaylistFormatTests.cs \
	Banshee.Services/Xspf/Xspf.cs \
	Hyena/CryptoUtilTests.cs \
	Hyena/RangeCollectionTests.cs

CFS=Hyena/RangeCollectionTests.cs Hyena/CryptoUtilTests.cs

NUNIT_TESTER_NAME = ConsoleUi
NUNIT_TESTER = $(NUNIT_TESTER_NAME).exe
NUNIT_TESTER_CSFILES =  \
	AssemblyInfo.cs \
	ConsoleUi.cs

$(ASSEMBLY): $(ASSEMBLY_CSFILES)
	$(MCS) $(MCS_FLAGS) $(NUNIT_FLAGS) -out:$@ -target:library -r:System.Xml $(LINK_HYENA_DEPS) $(CFS)

$(NUNIT_TESTER): $(NUNIT_TESTER_CSFILES)
	$(MCS) $(MCS_FLAGS) -out:$@ $(NUNIT_FLAGS) $(NUNIT_TESTER_CSFILES)

if ENABLE_TESTS
all: $(ASSEMBLY)

run-test: $(NUNIT_TESTER) $(ASSEMBLY)
	MONO_PATH="$(top_builddir)/bin" mono --debug $(NUNIT_TESTER) $(ASSEMBLY)
endif

CLEANFILES = $(ASSEMBLY) $(NUNIT_TESTER) TestResult.xml
DISTCLEANFILES = *.mdb *.dll *.exe
MAINTAINERCLEANFILES = Makefile.in

