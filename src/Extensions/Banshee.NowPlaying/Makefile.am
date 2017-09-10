ASSEMBLY = Banshee.NowPlaying
TARGET = library
LINK = $(REF_EXTENSION_NOWPLAYING)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.NowPlaying/FullscreenAdapter.cs \
	Banshee.NowPlaying/FullscreenControls.cs \
	Banshee.NowPlaying/FullscreenWindow.cs \
	Banshee.NowPlaying/IFullscreenAdapter.cs \
	Banshee.NowPlaying/IVideoDisplay.cs \
	Banshee.NowPlaying/NowPlayingContents.cs \
	Banshee.NowPlaying/NowPlayingInterface.cs \
	Banshee.NowPlaying/NowPlayingSource.cs \
	Banshee.NowPlaying/NowPlayingTrackInfoDisplay.cs \
	Banshee.NowPlaying/OverlayWindow.cs \
	Banshee.NowPlaying/VideoDisplay.cs \
	Banshee.NowPlaying/XOverlayVideoDisplay.cs

RESOURCES =  \
	Banshee.NowPlaying.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/idle-logo.png

if ENABLE_NOWPLAYING
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

