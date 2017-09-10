ASSEMBLY = Banshee.AmazonMp3
TARGET = exe
LINK = $(REF_EXTENSION_AMAZONMP3)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.AmazonMp3/AmazonDownloadManager.cs \
	Banshee.AmazonMp3/AmazonMp3DownloaderService.cs \
	Banshee.AmazonMp3/AmzMp3Downloader.cs \
	Banshee.AmazonMp3/AmzXspfPlaylist.cs \
	Banshee.AmazonMp3/ConsoleTool.cs \
	Banshee.AmazonMp3/ImportSource.cs \
	Banshee.AmazonMp3/Tests/AmzXspfPlaylistTest.cs

RESOURCES = Banshee.AmazonMp3.addin.xml

bamzdir = $(bindir)
bamz_SCRIPTS = bamz

mimedir = $(datadir)/mime/packages
mime_DATA = banshee-amz.xml

bamz: bamz.in
	@echo '  GEN   $@'; sed 's,@bansheedir@,$(pkglibdir),g' < $< > $@

if ENABLE_AMAZONMP3
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

CLEANFILES += bamz
EXTRA_DIST += bamz.in banshee-amz.xml
