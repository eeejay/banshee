include $(top_srcdir)/build/build.environment.mk

MCS_FLAGS = -debug
NUNIT_FLAGS = @MONO_NUNIT_LIBS@

ASSEMBLY_NAME = banshee-tests
ASSEMBLY = $(ASSEMBLY_NAME).dll
ASSEMBLY_CSFILES =  \
	$(srcdir)/Hyena/CryptoUtilTests.cs \
	$(srcdir)/Hyena/RangeCollectionTests.cs \
	$(srcdir)/Hyena/StringUtilTests.cs \
	$(srcdir)/Hyena/SqliteCommandTests.cs

#	Banshee.Core/KernelTests.cs
#	Banshee.Core/FileNamePatternTests.cs
#	Banshee.Services/PlaylistFormatTests.cs
#	Banshee.Services/Xspf/Xspf.cs

NUNIT_TESTER_NAME = ConsoleUi
NUNIT_TESTER = $(NUNIT_TESTER_NAME).exe
NUNIT_TESTER_CSFILES =  \
	AssemblyInfo.cs \
	ConsoleUi.cs

$(ASSEMBLY): $(ASSEMBLY_CSFILES)
	$(MCS) $(MCS_FLAGS) $(NUNIT_FLAGS) -out:$@ -target:library -r:System.Xml $(LINK_HYENA_DEPS) $(ASSEMBLY_CSFILES)

$(NUNIT_TESTER): $(NUNIT_TESTER_CSFILES)
	$(MCS) $(MCS_FLAGS) -out:$@ $(NUNIT_FLAGS) $(NUNIT_TESTER_CSFILES)

if ENABLE_TESTS
all: $(ASSEMBLY)

run-test: $(NUNIT_TESTER) $(ASSEMBLY)
	LANG=it_IT MONO_PATH="$(top_builddir)/bin" mono --debug $(NUNIT_TESTER) $(ASSEMBLY)
endif

EXTRA_DIST = $(ASSEMBLY_CSFILES)

CLEANFILES = $(ASSEMBLY) $(NUNIT_TESTER) TestResult.xml
DISTCLEANFILES = *.mdb *.dll *.exe
MAINTAINERCLEANFILES = Makefile.in

