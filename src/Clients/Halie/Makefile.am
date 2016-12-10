ASSEMBLY = Halie
TARGET = exe
LINK = $(REF_HALIE)
SOURCES = Halie/Client.cs 

if ENABLE_CLIENT_HALIE
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
