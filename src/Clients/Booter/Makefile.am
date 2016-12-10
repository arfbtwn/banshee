ASSEMBLY = Banshee
TARGET = exe
LINK = $(REF_BOOTER)
SOURCES = Booter/Entry.cs

include $(top_srcdir)/build/build.mk

bin_SCRIPTS = banshee
EXTRA_DIST += Banshee.exe.config
module_SCRIPTS += Banshee.exe.config

if PLATFORM_DARWIN
banshee: banshee.darwin
else
banshee: banshee.linux
endif
	cp $< $@

install-exec-hook:
	$(LN_S) -f banshee $(DESTDIR)$(bindir)/muinshee

CLEANFILES += banshee
