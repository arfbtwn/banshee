ASSEMBLY = Muinshee
TARGET = exe
LINK = $(REF_NEREID) $(LINK_EXTENSION_PLAYQUEUE_DEPS)
SOURCES =  \
	Muinshee/AlbumDialog.cs \
	Muinshee/BaseDialog.cs \
	Muinshee/Client.cs \
	Muinshee/MuinsheeActions.cs \
	Muinshee/MuinsheeTrackInfoDisplay.cs \
	Muinshee/PlayerInterface.cs \
	Muinshee/SongDialog.cs

RESOURCES = \
	Resources/GlobalUI.xml

if ENABLE_CLIENT_MUINSHEE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
