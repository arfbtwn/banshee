ASSEMBLY = Banshee.Torrent
TARGET = library
LINK = $(LINK_BANSHEE_SERVICES_DEPS) $(LINK_MIGO_DEPS) $(LINK_MONOTORRENT_DBUS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Torrent/TorrentService.cs \
	Banshee.Torrent/TorrentFileDownloadTask.cs

RESOURCES = Banshee.Torrent.addin.xml

EXTRA_BUNDLE = $(MONOTORRENT_ASSEMBLIES)

if ENABLE_TORRENT
include $(top_srcdir)/build/build.mk

install-data-hook:
	for ASM in $(MONOTORRENT_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(MONOTORRENT_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;

else
include $(top_srcdir)/build/build.dist.mk
endif
