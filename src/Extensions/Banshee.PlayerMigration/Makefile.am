ASSEMBLY = Banshee.PlayerMigration
TARGET = library
LINK = $(REF_EXTENSION_PLAYER_MIGRATION)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.PlayerMigration/AmarokPlayerImportSource.cs \
	Banshee.PlayerMigration/ItunesPlayerImportDialogs.cs \
	Banshee.PlayerMigration/ItunesPlayerImportSource.cs \
	Banshee.PlayerMigration/RhythmboxPlayerImportSource.cs

RESOURCES = Banshee.PlayerMigration.addin.xml

if ENABLE_PLAYERMIGRATION
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

