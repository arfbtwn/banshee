ASSEMBLY = Banshee.Audiobook
TARGET = library
LINK = $(REF_EXTENSION_AUDIOBOOK)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Audiobook/Actions.cs \
	Banshee.Audiobook/AudiobookContent.cs \
	Banshee.Audiobook/AudiobookFileNamePattern.cs \
	Banshee.Audiobook/AudiobookGrid.cs \
	Banshee.Audiobook/AudiobookLibrarySource.cs \
	Banshee.Audiobook/AudiobookModel.cs \
	Banshee.Audiobook/BookCover.cs \
	Banshee.Audiobook/BookPlaylist.cs \
	Banshee.Audiobook/BookView.cs

RESOURCES =  \
	Banshee.Audiobook.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml

if ENABLE_AUDIOBOOK
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
