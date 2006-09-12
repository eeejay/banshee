MCS_FLAGS = -debug

HYENA = hyena
HYENA_ASSEMBLY = $(HYENA).exe

HYENA_SOURCE = \
	$(top_srcdir)/src/SourceView.cs \
	HyenaEntry.cs \
	HyenaUserInterface.cs

HYENA_RESOURCES = 
HYENA_EXPANDED_RESOURCES = $(foreach resource,$(HYENA_RESOURCES), $(top_srcdir)/$(resource))

HYENA_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(HYENA_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

HYENA_SOURCE_BUILD = $(addprefix $(srcdir)/, $(HYENA_SOURCE))

HYENA_REFERENCES = \
	$(GTKSHARP_LIBS) \
	-r:Mono.Posix \
	-r:$(top_builddir)/hal-sharp/hal-sharp.dll \
	-r:$(top_builddir)/burn-sharp/burn-sharp.dll \
	-r:$(top_builddir)/src/MusicBrainz/MusicBrainz.dll \
	-r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll \
	-r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll

all: $(HYENA_ASSEMBLY)

$(HYENA_ASSEMBLY): $(HYENA_SOURCE_BUILD) $(HYENA_EXPANDED_RESOURCES)
	$(MCS) -out:$@ $(MCS_FLAGS) $(HYENA_REFERENCES) $(HYENA_EXPANDED_RESOURCES_BUILD) $(HYENA_SOURCE_BUILD)

MONO_PATH = MONO_PATH=../../hal-sharp:../../burn-sharp:../Last.FM:../Gnome.Keyring:../MusicBrainz:../Banshee.Base:../Banshee.Widgets 
	
run: $(HYENA_ASSEMBLY) 
	$(MONO_PATH) $(MONO) --debug --trace=disabled,N:Banshee.Base,Banshee,Banshee.Gui,Banshee.Source,Banshee.MediaEngine,Banshee.Gui.Dialogs $(HYENA_ASSEMBLY)

debug: $(HYENA_ASSEMBLY)
	$(MONO_PATH) gdb --eval-command=run --args $(MONO) --debug $(HYENA_ASSEMBLY)

trace:
	kill -USR2 `ps aux | grep hyena.exe | head -n 1 | awk 'BEGIN { } { print $$2 }'`

EXTRA_DIST = \
	$(HYENA_SOURCE) \
	$(HYENA_SOURCE_IN) \
	$(HYENA).in \
	$(HYENA_ASSEMBLY).config.in

CLEANFILES = $(HYENA) $(HYENA_ASSEMBLY) *.mdb *.dll *.pidb
MAINTAINERCLEANFILES = Makefile.in

