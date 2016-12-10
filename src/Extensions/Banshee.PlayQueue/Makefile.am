ASSEMBLY = Banshee.PlayQueue
TARGET = library
LINK = $(REF_EXTENSION_PLAYQUEUE)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.PlayQueue/HeaderWidget.cs \
	Banshee.PlayQueue/IPlayQueue.cs \
	Banshee.PlayQueue/PlayQueueActions.cs \
	Banshee.PlayQueue/PlayQueueSource.cs \
	Banshee.PlayQueue/PlayQueueTrackListModel.cs \
	Banshee.PlayQueue/QueueableSourceComboBox.cs \
	Banshee.PlayQueue/QueueMode.cs

RESOURCES =  \
	Banshee.PlayQueue.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml

if ENABLE_PLAYQUEUE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

