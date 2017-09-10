ASSEMBLY = MediaPanel
TARGET = exe
LINK = $(REF_MEDIAPANEL)
SOURCES = Banshee.MediaPanel.Client/MediaPanelClient.cs

if ENABLE_CLIENT_MEDIAPANEL
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
