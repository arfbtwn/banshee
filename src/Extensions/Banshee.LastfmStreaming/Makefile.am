ASSEMBLY = Banshee.LastfmStreaming
TARGET = library
LINK = $(REF_EXTENSION_LASTFM_STREAMING)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.LastfmStreaming/LastfmStreamingActions.cs \
	Banshee.LastfmStreaming/LastfmStreamingService.cs \
	Banshee.LastfmStreaming/LastfmTrackInfo.cs \
	Banshee.LastfmStreaming/StationEditor.cs \
	Banshee.LastfmStreaming/StationSource.cs \
	Banshee.LastfmStreaming/StationType.cs

RESOURCES =  \
	Banshee.LastfmStreaming.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml \
	Resources/LastfmTrackActions.xml

if ENABLE_LASTFMSTREAMING
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

