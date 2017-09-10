ASSEMBLY = Banshee.Lastfm
TARGET = library
LINK = $(REF_EXTENSION_LASTFM) -r:Mono.Security
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Lastfm.Audioscrobbler/AudioscrobblerService.cs \
	Banshee.Lastfm.Audioscrobbler/Queue.cs \
	Banshee.Lastfm.Recommendations/ContextPage.cs \
	Banshee.Lastfm.Recommendations/RecommendationPane.cs \
	Banshee.Lastfm.Recommendations/SimilarArtistTile.cs \
	Banshee.Lastfm/LastfmActions.cs \
	Banshee.Lastfm/LastfmColumnController.cs \
	Banshee.Lastfm/LastfmPreferences.cs \
	Banshee.Lastfm/LastfmSource.cs \
	Banshee.Lastfm/LastfmSourceContents.cs


RESOURCES =  \
	Banshee.Lastfm.addin.xml \
	Resources/AudioscrobblerMenu.xml \
	Resources/GlobalUI.xml \
	Resources/no-results.png \
	Resources/RecommendationMenu.xml

if ENABLE_LASTFM
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

