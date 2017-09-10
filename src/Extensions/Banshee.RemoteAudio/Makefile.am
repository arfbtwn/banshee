ASSEMBLY = Banshee.RemoteAudio
TARGET = library
LINK = $(REF_EXTENSION_REMOTE_AUDIO)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = \
	Banshee.RemoteAudio/RemoteAudioService.cs \
	Banshee.RemoteAudio/RemoteAudioActions.cs \
	Banshee.RemoteAudio/RemoteSpeakersComboBox.cs \
	Banshee.RemoteAudio/RemoteSpeakersWidget.cs \
	Banshee.RemoteAudio/RemoteSpeaker.cs

RESOURCES = \
    Banshee.RemoteAudio.addin.xml \
    Resources/GlobalUI.xml

if ENABLE_REMOTEAUDIO
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
