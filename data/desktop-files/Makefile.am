desktopdir = $(datadir)/applications
desktop_in_files = $(wildcard *.desktop.in.in)
desktop_DATA = $(desktop_in_files:.desktop.in.in=.desktop)

DESKTOP_FRAMEWORK_DEPS = common.desktop.in update-desktop-file.sh
DESKTOP_MIME_DEPS = $(wildcard *.desktop.mime)

if ENABLE_RELEASE
UPDATE_MIME_FILES = no
else
UPDATE_MIME_FILES = yes
endif

%.desktop: %.desktop.in
	$(AM_V_GEN)$(MSGFMT) --desktop --template $< -d $(top_srcdir)/po -o $@

%.desktop.in: %.desktop.in.in $(DESKTOP_FRAMEWORK_DEPS) $(DESKTOP_MIME_DEPS)
	./update-desktop-file.sh "$<" "$(VERSION)" "$(UPDATE_MIME_FILES)" > "$@"

install-data-hook:
	test -f $(DESTDIR)$(datadir)/applications/defaults.list && \
	test -z $(DESTDIR) && update-desktop-database $(DESTDIR)$(datadir)/applications || :

EXTRA_DIST = \
	$(desktop_in_files) \
	$(DESKTOP_MIME_DEPS) \
	$(DESKTOP_FRAMEWORK_DEPS)

CLEANFILES = \
	$(desktop_DATA)

MAINTAINERCLEANFILES = \
	Makefile.in \
	common.desktop

