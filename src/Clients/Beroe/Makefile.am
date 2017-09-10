ASSEMBLY = Beroe
TARGET = exe
LINK = $(REF_BEROE)
SOURCES = Beroe/IndexerClient.cs

if ENABLE_CLIENT_BEROE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif

