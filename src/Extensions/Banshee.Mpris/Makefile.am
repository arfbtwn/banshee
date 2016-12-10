ASSEMBLY = Banshee.Mpris
TARGET = library
LINK = $(REF_EXTENSION_MPRIS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Mpris/IMediaPlayer.cs \
	Banshee.Mpris/IPlayer.cs \
	Banshee.Mpris/IPlaylists.cs \
	Banshee.Mpris/MediaPlayer.cs \
	Banshee.Mpris/Metadata.cs \
	Banshee.Mpris/MprisService.cs

RESOURCES = Banshee.Mpris.addin.xml

if ENABLE_MPRIS
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

