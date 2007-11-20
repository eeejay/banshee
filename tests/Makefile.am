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
	Hyena/SelectionTests.cs

NUNIT_TESTER_NAME = ConsoleUi
NUNIT_TESTER = $(NUNIT_TESTER_NAME).exe
NUNIT_TESTER_CSFILES =  \
	AssemblyInfo.cs \
	ConsoleUi.cs

$(ASSEMBLY): $(ASSEMBLY_CSFILES)
	$(MCS) $(MCS_FLAGS) $(NUNIT_FLAGS) -out:$@ -target:library -r:System.Xml $(LINK_BANSHEE_SERVICES_DEPS) $(ASSEMBLY_CSFILES)

$(NUNIT_TESTER): $(NUNIT_TESTER_CSFILES)
	$(MCS) $(MCS_FLAGS) -out:$@ $(NUNIT_FLAGS) $(NUNIT_TESTER_CSFILES)

if ENABLE_TESTS
run-test: $(NUNIT_TESTER) $(ASSEMBLY)
	MONO_PATH="$(RUN_PATH)" mono --debug $(NUNIT_TESTER) $(ASSEMBLY)
endif

CLEANFILES = $(ASSEMBLY) $(NUNIT_TESTER) TestResult.xml
DISTCLEANFILES = *.mdb *.dll *.exe
MAINTAINERCLEANFILES = Makefile.in

