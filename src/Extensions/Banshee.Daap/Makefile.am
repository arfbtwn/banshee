ASSEMBLY = Banshee.Daap
TARGET = library
LINK = $(REF_EXTENSION_DAAP)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Daap/DaapContainerSource.cs \
	Banshee.Daap/DaapErrorView.cs \
	Banshee.Daap/DaapLoginDialog.cs \
	Banshee.Daap/DaapPlaylistSource.cs \
	Banshee.Daap/DaapProxyWebServer.cs \
	Banshee.Daap/DaapResolverJob.cs \
	Banshee.Daap/DaapService.cs \
	Banshee.Daap/DaapSource.cs \
	Banshee.Daap/DaapTrackInfo.cs \
	Banshee.Daap/OpenRemoteServer.cs \
	Daap/AuthenticationException.cs \
	Daap/BrokenMD5.cs \
	Daap/Client.cs \
	Daap/ContentCodeBag.cs \
	Daap/ContentFetcher.cs \
	Daap/ContentParser.cs \
	Daap/ContentWriter.cs \
	Daap/Database.cs \
	Daap/Hasher.cs \
	Daap/LoginException.cs \
	Daap/Playlist.cs \
	Daap/ServerInfo.cs \
	Daap/ServiceLocator.cs \
	Daap/Track.cs \
	Daap/User.cs \
	Daap/Utility.cs

RESOURCES =  \
	Banshee.Daap.addin.xml \
	Daap/content-codes \
	Resources/GlobalUI.xml

if ENABLE_DAAP
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
