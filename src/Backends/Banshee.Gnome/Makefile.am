ASSEMBLY = Banshee.Gnome
TARGET = library
LINK = $(REF_BACKEND_GNOME)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.GnomeBackend/Brasero.cs \
	Banshee.GnomeBackend/DiscDuplicator.cs \
	Banshee.GnomeBackend/GnomeScreensaverManager.cs \
	Banshee.GnomeBackend/GnomeService.cs \
	Banshee.GnomeBackend/GSettingsConfigurationClient.cs \
	Banshee.GnomeBackend/ProxyConfiguration.cs

RESOURCES =  \
	Banshee.Gnome.addin.xml \
	Resources/GlobalUI.xml

if ENABLE_GNOME
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

