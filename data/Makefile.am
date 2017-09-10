SUBDIRS = \
	audio-profiles \
	desktop-files \
	icon-theme-hicolor

dbusservice_in_files = \
	org.bansheeproject.Banshee.service.in \
	org.bansheeproject.CollectionIndexer.service.in
dbusservicedir = $(datadir)/dbus-1/services
dbusservice_DATA = $(dbusservice_in_files:.service.in=.service)

# Generate a fake source file containing strings for
# translation that are found in our .addin.xml files
ADDIN_XML_FILES = $(shell find $(top_srcdir)/src -name \*.addin.xml | grep -v /obj/)
addin-xml-strings.cs: $(ADDIN_XML_FILES) $(top_builddir)/build/addin-xml-string-extractor.exe
	@echo "  GEN   $@"
	$(MONO) $(top_builddir)/build/addin-xml-string-extractor.exe $(ADDIN_XML_FILES) > $@

appdatadir = $(datadir)/appdata
appdata_DATA = $(appdata_in_files:.xml.in=.xml)
appdata_in_files = banshee.appdata.xml.in

%.appdata.xml: %.appdata.xml.in
	$(AM_V_GEN)$(MSGFMT) --xml --template $< -d $(top_srcdir)/po -o $@

all: addin-xml-strings.cs

EXTRA_DIST = \
	$(dbusservice_in_files) \
	addin-xml-strings.cs \
	$(appdata_in_files)

CLEANFILES = \
	$(appdata_DATA)

DISTCLEANFILES = \
	$(dbusservice_DATA)

MAINTAINERCLEANFILES = \
	Makefile.in

