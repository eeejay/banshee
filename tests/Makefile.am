include $(top_srcdir)/build/build.environment.mk

if ENABLE_TESTS

TEST_ASSEMBLIES = \
	Hyena.dll \
	Migo.dll \
	Banshee.Core.dll \
	Banshee.Services.dll \
	Banshee.Dap.Mtp.dll

RUNNER = for asm in $${TEST_ASSEMBLIES}; do echo -e "\033[1mRunning tests on $${asm}...\033[0m"; TZ=America/Chicago LC_ALL=it_IT LANG=it_IT nunit-console2 /nologo $$asm; done

test:
	@pushd $(DIR_BIN) &>/dev/null; \
	export TEST_ASSEMBLIES="$(TEST_ASSEMBLIES)"; $(RUNNER); \
	popd &>/dev/null;

%:
	@pushd $(DIR_BIN) &>/dev/null; \
	for i in *.dll; do \
		if [[ $$(echo "$$i" | tr "[:upper:]" "[:lower:]") = "$$(echo "$@" | tr "[:upper:]" "[:lower:]").dll" ]]; then \
			export TEST_ASSEMBLIES="$$i"; $(RUNNER); \
		fi; \
	done;

endif

MAINTAINERCLEANFILES = Makefile.in

