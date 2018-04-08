SUBDIRS = \
	build \
	data \
	help \
	src \
	docs \
	tests \
	extras \
	po

# Important targets

clean-local:
	rm -rf $(top_builddir)/bin

uninstall-hook:
	if test -d $(pkglibdir); then \
		rmdir $(pkglibdir); \
	fi

dist-hook:
	@( \
	  echo "`date +%Y-%m-%d`  The Banshee Project  <http://banshee.fm>"; \
	  echo; \
	  echo "@Banshee $(VERSION)"; \
	  echo; \
	  echo "@You probably are looking for the useful change summary detailed"; \
	  echo "@in the NEWS file, perfect for downstream maintainers."; \
	  echo; \
	  echo "@If you care about commit-level changes, use 'git log' against"; \
	  echo "@a clone of our git repository (git://git.gnome.org/banshee)."; \
	  echo; \
	) | tr '@' '\t' > $(top_distdir)/ChangeLog

# Developer targets

LC_BUILD = $(top_builddir)/bin/share/locale
PO_FILES = $(addprefix po/, $(addsuffix .gmo, $(ALL_LINGUAS)))
MO_FILES = $(patsubst po/%.gmo, $(LC_BUILD)/%/LC_MESSAGES/$(PACKAGE).mo, $(PO_FILES))

$(MO_FILES): $(LC_BUILD)/%/LC_MESSAGES/$(PACKAGE).mo: po/%.gmo
	$(MKDIR_P) $(dir $@)
	cp -rf '$<' '$@'

check-bootstrap-env:
	@if test -z "$$BOCKBUILD_ENV"; then \
		for env in $$(find $(top_srcdir) -maxdepth 1 -name \*.env); do \
			echo "A bootstrap environment was found, but it does not"; \
			echo "appear to be sourced into your current environment:"; \
			echo; \
			echo "    $$env"; \
			echo; \
			echo "Source the environment first."; \
			echo; \
			exit 1; \
		done; \
	fi

PROFILE_DEFAULT=--profile=log:time=fast,calls,noalloc,maxframes=5
PROFILE_HEAP=--profile=logging:heap=all,gc-dumps=-1
PROFILE_GUI=--profile=gui-thread-check

run: check-bootstrap-env $(MO_FILES)
	(cd bin; $(MONO) --debug $(PROFILE_GUI) $(BANSHEE_DEV_MONO_OPTIONS) Nereid.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS))

run-profiled: check-bootstrap-env $(MO_FILES)
	(cd bin; LD_LIBRARY_PATH=$(LD_LIBRARY_PATH):/usr/local/lib $(MONO) --debug $(BANSHEE_DEV_MONO_OPTIONS) $(PROFILE_DEFAULT) Nereid.exe --uninstalled $(BANSHEE_DEV_OPTIONS))

run-logged: check-bootstrap-env $(MO_FILES)
	(cd bin; $(MONO) --debug $(BANSHEE_DEV_MONO_OPTIONS) Nereid.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS) | tee last-run-log)

run-mediapanel: check-bootstrap-env $(MO_FILES)
	(cd bin; $(MONO) --debug $(BANSHEE_DEV_MONO_OPTIONS) MediaPanel.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS))

run-muinshee: check-bootstrap-env $(MO_FILES)
	(cd bin; $(MONO) --debug $(BANSHEE_DEV_MONO_OPTIONS) Muinshee.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS))

run-perf: run-logged
	extras/perf-analyze-log bin/last-run-log

run-trace: $(MO_FILES)
	(cd bin; $(MONO) \
		--trace=$$(find . -name "*dll" | sed 's/\.\///' | sed 's/\.dll//' | sort | tr "\n" , | sed 's/,$$//') \
		Nereid.exe --uninstalled $(BANSHEE_DEV_OPTIONS) > ../banshee.trace)

run-profile-heap: $(MO_FILES)
	(cd bin; $(MONO) $(PROFILE_HEAP) --debug $(BANSHEE_DEV_MONO_OPTIONS) Nereid.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS))

query-trace:
	@echo "Did you set BANSHEE_DEV_MONO_OPTIONS to --trace=disabled?"; \
	MONO_PID=$$(ps aux | grep mono | grep '\--uninstalled' | grep -v 'cd bin' | awk '{print$$2}'); \
	kill -USR2 $$MONO_PID

gdb:
	@pushd bin; \
	gdb mono --eval-command="handle SIGXCPU SIG35 SIGPWR nostop noprint" --eval-command="b g_return_if_fail_warning" --eval-command="r --debug Nereid.exe --debug --uninstalled"; \
	popd;

mdb:
	@pushd bin; \
	mdb -run Nereid.exe --debug --uninstalled $(BANSHEE_DEV_OPTIONS); \
	popd;

csharp:
	@pushd .; \
    echo ""; \
    echo "Starting csharp with Gtk#, Hyena, Banshee, etc referenced"; \
    csharp -lib:bin/ -pkg:glib-sharp-2.0 -pkg:gtk-sharp-2.0 -pkg:dbus-sharp -pkg:taglib-sharp -pkg:ipod-sharp -pkg:mono-addins $(addprefix "-reference:", $(wildcard bin/*.dll)); \
	popd;

test: all
	@pushd tests; \
	make test \
	popd;

hg:
	pushd bin; \
	echo "class Entry { static void Main () { Hyena.Gui.TestModuleRunner.Run (); } }" > tester.cs; \
	$(MCS) -r:Hyena.Gui.dll tester.cs; \
	mono --debug tester.exe; \
	rm tester.*; \
	popd; 

update-docs:
	make -C docs/Hyena update-docs; \
	make -C docs/Banshee update-docs;

merge-docs:
	make -C docs/Hyena merge; \
	make -C docs/Banshee merge;

check-since:
	@if [ -z '$(SINCE)' ]; then echo 'Specify a SINCE (e.g. make package-patch SINCE=1.6.0)'; exit 1; fi;

package-patch: check-since
	@if [ -f 'since-$(SINCE).patch' ]; then echo 'since-$(SINCE).patch already exists'; exit 1; fi;
	@git diff-tree -p "$(SINCE).." | filterdiff \
		-x 'a/Makefile.am' \
		-x 'a/profile-configure' \
		-x 'a/po/*' \
		-x 'a/banshee.doap' \
		-x \*.csproj \
		-x \*.sln \
		-x \*extras/\* &> 'since-$(SINCE).patch'
	@echo 'since-$(SINCE).patch is ready'

package-po: check-since
	@if [ -f 'po-since-$(SINCE).tar.bz2' ]; then echo 'po-since-$(SINCE).tar.bz2 already exists'; exit 1; fi;
	@git diff-tree -p '$(SINCE)..' po \
		| awk -F / '{if ($$0 ~ /^+++ b\/po\/.+\.po$$/) print $$2 "/" $$3}' \
		| xargs tar cfj po-since-$(SINCE).tar.bz2 2>/dev/null; \
	if [ $$? -eq 123 ]; then \
		echo 'No po changes since $(SINCE)'; \
		exit 0; \
	fi

EXTRA_DIST = \
	intltool-extract.in \
	intltool-merge.in \
	intltool-update.in \
	autogen.sh \
	AUTHORS \
	HACKING

CLEANFILES = \
	intltool-extract \
	intltool-update \
	intltool-merge

DISTCLEANFILES = \
	*.bak \
	*~ \
	*.pidb

MAINTAINERCLEANFILES = \
	compile \
	INSTALL \
	config.h.in \
	aclocal.m4 \
	ltmain.sh \
	Makefile.in \
	depcomp \
	missing \
	install-sh \
	configure \
	config.sub \
	config.guess \
	intltool-extract.in \
	intltool-merge.in   \
	intltool-update.in  \
	mkinstalldirs
