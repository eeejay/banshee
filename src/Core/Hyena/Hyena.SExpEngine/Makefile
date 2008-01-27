VERSION=0.1
ASSEMBLY = SExpEngine.dll
SOURCES = src/SExpEngine/*.cs
SVN_BASE = svn+ssh://abock@svn.gnome.org/svn/banshee
MCS = gmcs -debug -nowarn:0278

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES)
	$(MCS) -out:$@ -target:library $(SOURCES)

clean:
	-rm $(ASSEMBLY){,.mdb}

tag:
	svn cp \
		$(SVN_BASE)/trunk/sexp-engine \
		$(SVN_BASE)/tags/sexp-engine/$(VERSION) \
		-m "Tag for sexp-engine version $(VERSION)"

