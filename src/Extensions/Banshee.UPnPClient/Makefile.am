ASSEMBLY = Banshee.UPnPClient
TARGET = library
LINK = $(REF_EXTENSION_UPNP)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.UPnPClient/UPnPContainerSource.cs \
	Banshee.UPnPClient/UPnPMusicSource.cs \
	Banshee.UPnPClient/UPnPServerSource.cs \
	Banshee.UPnPClient/UPnPService.cs \
	Banshee.UPnPClient/UPnPTrackInfo.cs \
	Banshee.UPnPClient/UPnPVideoSource.cs

RESOURCES = Banshee.UPnPClient.addin.xml

EXTRA_BUNDLE = $(MONOUPNP_ASSEMBLIES)

if ENABLE_UPNP
include $(top_srcdir)/build/build.mk

define EXTRA_INSTALL_DATA_HOOK
	for ASM in $(MONOUPNP_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;
endef

define EXTRA_UNINSTALL_HOOK
	for ASM in $(MONOUPNP_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;
endef

else
include $(top_srcdir)/build/build.dist.mk
endif
