ASSEMBLY = Nereid
TARGET = exe
LINK = $(REF_NEREID)
SOURCES =  \
	Nereid/Client.cs \
	Nereid/PlayerInterface.cs \
	Nereid/ViewContainer.cs

if ENABLE_CLIENT_NEREID
include $(top_srcdir)/build/build.mk
else
include $(top_srcdir)/build/build.dist.mk
endif
