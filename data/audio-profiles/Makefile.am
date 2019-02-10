audioprofilesdir = $(pkgdatadir)/audio-profiles
audioprofiles_in_files = \
	aac.xml.in \
	aac-novell.xml.in \
	base.xml.in \
	flac.xml.in \
	mp3-lame.xml.in \
	mp3-xing.xml.in \
	vorbis.xml.in \
	wavpack.xml.in \
	wav.xml.in \
	wma.xml.in
audioprofiles_DATA = $(audioprofiles_in_files:.xml.in=.xml)

%.xml: %.xml.in
	GETTEXTDATADIR=$(top_builddir) $(AM_V_GEN)$(MSGFMT) --xml --template $< -d $(top_srcdir)/po -o $@

all: $(audioprofiles_DATA)
	mkdir -p $(top_builddir)/bin/share/$(PACKAGE)/audio-profiles && cp $(audioprofiles_DATA) $(top_builddir)/bin/share/$(PACKAGE)/audio-profiles

EXTRA_DIST = $(audioprofiles_in_files)
DISTCLEANFILES = *.xml
MAINTAINERCLEANFILES = Makefile.in

