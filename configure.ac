dnl Granularly define the version components
dnl Remember to also update the Display Version
m4_define([banshee_version_major], [2])
m4_define([banshee_version_minor], [9])
m4_define([banshee_version_micro], [2])

dnl this can sometimes differ manually
m4_define([banshee_display_version], ["2.9"])

dnl set to 0 when doing an official release
m4_define([banshee_version_pre_release], [0])

m4_define([concat], $1$2$3$4)

dnl create base version string
m4_define([banshee_version], concat(banshee_version_major., banshee_version_minor., banshee_version_micro))

dnl create the release version
m4_define([banshee_version],
	m4_if(banshee_version_pre_release, [0],
		banshee_version,
		concat(banshee_version, banshee_version_pre_release)))

m4_define([banshee_api_version], [banshee_version_major.banshee_version_minor])
m4_define([banshee_asm_version], [banshee_api_version.0.0])

AC_PREREQ(2.52)
AC_INIT([banshee], banshee_version, [http://bugzilla.gnome.org/enter_bug.cgi?product=banshee])

AM_INIT_AUTOMAKE([1.10 no-dist-gzip dist-bzip2 tar-ustar foreign])
AM_MAINTAINER_MODE([enable])
AC_CONFIG_HEADERS([config.h])
AC_CONFIG_MACRO_DIRS([build/m4 build/m4/banshee build/m4/banshee/backends build/m4/banshee/clients build/m4/banshee/dap build/m4/banshee/extensions build/m4/shamrock build/m4/shave])

AC_PROG_LIBTOOL
AC_PROG_INSTALL

dnl How to configure for distcheck
DISTCHECK_CONFIGURE_FLAGS="--disable-docs --enable-mtp --enable-daap --enable-user-help --enable-release"
AC_SUBST(DISTCHECK_CONFIGURE_FLAGS)

dnl Export Version Info
AC_SUBST([API_VERSION], [banshee_api_version])
AC_SUBST([ASM_VERSION], [banshee_asm_version])
AC_SUBST([DISPLAY_VERSION], [banshee_display_version])

dnl Expansions
SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

PKG_CHECK_MODULES(GLIB, glib-2.0 >= 2.22)

dnl User help
SHAMROCK_CHECK_GNOME_DOC_UTILS(0.17.3)

dnl Mono and mcs
SHAMROCK_CHECK_MONO_MODULE(3.2.8)
SHAMROCK_FIND_MONO_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_CHECK_MONO_4_5_GAC_ASSEMBLIES([
	Mono.Posix
	ICSharpCode.SharpZipLib
])

BANSHEE_DESKTOP_NAME="Name=Banshee"
AC_SUBST(BANSHEE_DESKTOP_NAME)

dnl Required dependencies (managed/core)
BANSHEE_CHECK_GTK_SHARP

dnl Clutter support is not available in Gst# backend and is opt-in in the unmanaged one
dnl so we set a default value here for simplicity
AM_CONDITIONAL(HAVE_CLUTTER, false)

dnl Media backends
BANSHEE_CHECK_GSTREAMER

dnl DBus Sharp libraries
BANSHEE_CHECK_DBUS_SHARP

dnl Mono.Addins libraries
BANSHEE_CHECK_MONO_ADDINS

dnl Monodoc (optional through --disable-docs)
SHAMROCK_CHECK_MONODOC

dnl webkit (optional through --disable-webkit)
BANSHEE_CHECK_LIBWEBKIT

dnl NUnit (optional)
SHAMROCK_CHECK_NUNIT

dnl External TagLib# libraries (optional)
BANSHEE_CHECK_TAGLIB_SHARP

dnl Platform support
BANSHEE_CHECK_UNIX
BANSHEE_CHECK_WINDOWS
BANSHEE_CHECK_OSX

dnl GNOME (optional)
BANSHEE_CHECK_GNOME

PKG_CHECK_MODULES(SQLITE, sqlite3 >= 3.8.1)

dnl Clients
BANSHEE_CHECK_CLIENT_NEREID
BANSHEE_CHECK_CLIENT_HALIE
BANSHEE_CHECK_CLIENT_BEROE
BANSHEE_CHECK_CLIENT_MEDIAPANEL
BANSHEE_CHECK_CLIENT_MUINSHEE

dnl Hardware: gio#
BANSHEE_CHECK_GIO_SHARP

dnl DAP support (each module is optional)
BANSHEE_CHECK_DAP_MASS_STORAGE
BANSHEE_CHECK_DAP_MTP
BANSHEE_CHECK_DAP_APPLEDEVICE
BANSHEE_CHECK_DAP_KARMA

dnl DAAP support (optional)
BANSHEE_CHECK_DAAP

dnl Extensions
BANSHEE_CHECK_EXTENSION_AMAZONMP3
BANSHEE_CHECK_EXTENSION_AMAZONMP3_STORE
BANSHEE_CHECK_EXTENSION_AUDIOBOOK
BANSHEE_CHECK_EXTENSION_BOOSCRIPT
BANSHEE_CHECK_EXTENSION_BPM
BANSHEE_CHECK_EXTENSION_COVERART
BANSHEE_CHECK_EXTENSION_DAAP
BANSHEE_CHECK_EXTENSION_EMUSIC
BANSHEE_CHECK_EXTENSION_EMUSIC_STORE
BANSHEE_CHECK_EXTENSION_FILESYSTEMQUEUE
BANSHEE_CHECK_EXTENSION_FIXUP
BANSHEE_CHECK_EXTENSION_INTERNETARCHIVE
BANSHEE_CHECK_EXTENSION_INTERNETRADIO
BANSHEE_CHECK_EXTENSION_LASTFM
BANSHEE_CHECK_EXTENSION_LASTFMSTREAMING
BANSHEE_CHECK_EXTENSION_LIBRARYWATCHER
BANSHEE_CHECK_EXTENSION_MEDIAPANEL
BANSHEE_CHECK_EXTENSION_MINIMODE
BANSHEE_CHECK_EXTENSION_MIROGUIDE
BANSHEE_CHECK_EXTENSION_MPRIS
BANSHEE_CHECK_EXTENSION_MULTIMEDIAKEYS
BANSHEE_CHECK_EXTENSION_NOTIFICATIONAREA
BANSHEE_CHECK_EXTENSION_NOWPLAYING
BANSHEE_CHECK_EXTENSION_OPTICALDISC
BANSHEE_CHECK_EXTENSION_PLAYERMIGRATION
BANSHEE_CHECK_EXTENSION_PLAYQUEUE
BANSHEE_CHECK_EXTENSION_PODCASTING
BANSHEE_CHECK_EXTENSION_REMOTEAUDIO
BANSHEE_CHECK_EXTENSION_SAMPLE
BANSHEE_CHECK_EXTENSION_SOUNDMENU
BANSHEE_CHECK_EXTENSION_SQLDEBUGCONSOLE
BANSHEE_CHECK_EXTENSION_TORRENT
BANSHEE_CHECK_EXTENSION_UBUNTUONE
BANSHEE_CHECK_EXTENSION_UPNP
BANSHEE_CHECK_EXTENSION_WIKIPEDIA
BANSHEE_CHECK_EXTENSION_YOUTUBE

dnl i18n
SHAMROCK_CONFIGURE_I18N($PACKAGE)
AM_GNU_GETTEXT([external])
AM_GNU_GETTEXT_VERSION([0.19.6])
AM_GNU_GETTEXT_REQUIRE_VERSION([0.19.6])

AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use release configuration]),
	enable_release=yes, enable_release=`(test ! -d .svn && test ! -d .git) && echo "yes" || echo "no"`)
AM_CONDITIONAL(ENABLE_RELEASE, test "x$enable_release" = "xyes")
ENABLE_RELEASE=$enable_release
AC_SUBST(ENABLE_RELEASE)
if test "x$enable_release" = "xyes"; then
	MCS_FLAGS="-define:RELEASE"
else
	MCS_FLAGS=""
fi
#FIXME: add "-warnaserror" back above!

AC_SUBST(MCS_FLAGS)

AM_CONDITIONAL(HYENA_PKG_CONFIG, false)

AC_ARG_WITH(vendor-build-id,
	AC_HELP_STRING([--with-vendor-build-id=<vendor-build-id>],
		[Set a vendor build ID, recommended for packagers]),
	[vendor_build_id="$withval"], [
		test -d .git \
			&& vendor_build_id="git-checkout" \
			|| vendor_build_id="source-tarball"
	])
BUILD_VENDOR_ID="$vendor_build_id"
BUILD_HOST_OS="$host_os"
BUILD_HOST_CPU="$host_cpu"
BUILD_TIME=`date +"%F %T %Z"`
AC_SUBST(BUILD_VENDOR_ID)
AC_SUBST(BUILD_HOST_OS)
AC_SUBST(BUILD_HOST_CPU)
AC_SUBST(BUILD_TIME)

SHAVE_INIT([build/m4/shave], [enable])

dnl generated files
AC_OUTPUT([
Makefile

build/Makefile
build/m4/Makefile
build/m4/shave/shave
build/m4/shave/shave-libtool
build/pkg-config/Makefile

data/Makefile
data/org.bansheeproject.Banshee.service
data/org.bansheeproject.CollectionIndexer.service
data/audio-profiles/Makefile
data/desktop-files/Makefile
data/desktop-files/common.desktop
data/icon-theme-hicolor/Makefile

help/Makefile

docs/Makefile
docs/Hyena/Makefile
docs/Banshee/Makefile

po/Makefile.in

tests/Makefile
tests/Analyzer/Makefile
tests/Performance/Makefile

extras/Makefile

src/Hyena/Makefile
src/Hyena/build/Makefile
src/Hyena/build/pkg-config/Makefile
src/Hyena/build/m4/Makefile
src/Hyena/Hyena/Makefile
src/Hyena/Hyena.Data.Sqlite/Makefile
src/Hyena/Hyena.Gui/Makefile

src/Makefile
src/AssemblyInfo.cs

src/Clients/Makefile
src/Clients/Beroe/Makefile
src/Clients/Booter/Makefile
src/Clients/Booter/banshee.linux
src/Clients/Booter/banshee.darwin
src/Clients/Halie/Makefile
src/Clients/Muinshee/Makefile
src/Clients/Nereid/Makefile
src/Clients/MediaPanel/Makefile

src/Core/Makefile
src/Core/Banshee.CollectionIndexer/Makefile
src/Core/Banshee.Core/Makefile
src/Core/Banshee.Services/Makefile
src/Core/Banshee.ThickClient/Makefile
src/Core/Banshee.Widgets/Makefile
src/Core/Banshee.WebBrowser/Makefile
src/Core/Banshee.WebBrowser/libossifer/Makefile

src/Backends/Makefile
src/Backends/Banshee.Gio/Makefile
src/Backends/Banshee.Gnome/Makefile
src/Backends/Banshee.GStreamer/Makefile
src/Backends/Banshee.GStreamer/libbanshee/Makefile
src/Backends/Banshee.GStreamerSharp/Makefile
src/Backends/Banshee.Unix/Makefile
src/Backends/Banshee.Osx/Makefile
src/Backends/Banshee.Windows/Makefile

src/Libraries/Makefile
src/Libraries/Lastfm/Makefile
src/Libraries/Lastfm.Gui/Makefile
src/Libraries/Migo/Makefile
src/Libraries/Mono.Media/Makefile
src/Libraries/Mtp/Makefile
src/Libraries/Mtp/Mtp.dll.config
src/Libraries/MusicBrainz/Makefile
src/Libraries/Notifications/Makefile

src/Dap/Makefile
src/Dap/Banshee.Dap/Makefile
src/Dap/Banshee.Dap.AppleDevice/Makefile
src/Dap/Banshee.Dap.MassStorage/Makefile
src/Dap/Banshee.Dap.Mtp/Makefile
src/Dap/Banshee.Dap.Karma/Makefile

src/Extensions/Makefile
src/Extensions/Banshee.AmazonMp3/Makefile
src/Extensions/Banshee.AmazonMp3.Store/Makefile
src/Extensions/Banshee.Audiobook/Makefile
src/Extensions/Banshee.BooScript/Makefile
src/Extensions/Banshee.Bpm/Makefile
src/Extensions/Banshee.CoverArt/Makefile
src/Extensions/Banshee.Daap/Makefile
src/Extensions/Banshee.Emusic/Makefile
src/Extensions/Banshee.Emusic.Store/Makefile
src/Extensions/Banshee.FileSystemQueue/Makefile
src/Extensions/Banshee.InternetArchive/Makefile
src/Extensions/Banshee.Fixup/Makefile
src/Extensions/Banshee.InternetRadio/Makefile
src/Extensions/Banshee.Lastfm/Makefile
src/Extensions/Banshee.LastfmStreaming/Makefile
src/Extensions/Banshee.LibraryWatcher/Makefile
src/Extensions/Banshee.MiniMode/Makefile
src/Extensions/Banshee.MiroGuide/Makefile
src/Extensions/Banshee.MediaPanel/Makefile
src/Extensions/Banshee.Mpris/Makefile
src/Extensions/Banshee.MultimediaKeys/Makefile
src/Extensions/Banshee.NotificationArea/Makefile
src/Extensions/Banshee.NowPlaying/Makefile
src/Extensions/Banshee.OpticalDisc/Makefile
src/Extensions/Banshee.PlayQueue/Makefile
src/Extensions/Banshee.PlayerMigration/Makefile
src/Extensions/Banshee.Podcasting/Makefile
src/Extensions/Banshee.SoundMenu/Makefile
src/Extensions/Banshee.SqlDebugConsole/Makefile
src/Extensions/Banshee.Sample/Makefile
src/Extensions/Banshee.Torrent/Makefile
src/Extensions/Banshee.RemoteAudio/Makefile
src/Extensions/Banshee.UbuntuOneMusicStore/Makefile
src/Extensions/Banshee.UPnPClient/Makefile
src/Extensions/Banshee.Wikipedia/Makefile
src/Extensions/Banshee.YouTube/Makefile

extras/metrics/Makefile
])

AC_MSG_NOTICE([${PACKAGE}-${VERSION}

  Build Environment
    Install Prefix:        ${prefix}
    Datadir:               ${expanded_datadir}
    Libdir:                ${expanded_libdir}

    C Compiler:            ${CC}
    Mono C# Compiler:      ${MCS} ${MCS_FLAGS}
    Mono Runtime:          ${MONO}
    Gtk#:                  ${gtk_sharp_version}

  Media/Graphics:
    GStreamer# backend:    ${enable_gst_sharp}
    GStreamer C backend:   ${enable_gst_native}
    Clutter:               ${enable_clutter}

  Platform Support:
    UNIX Support:          ${enable_unix}
    Windows Support:       ${enable_windows}
    OSX Support:           ${enable_osx}

    GNOME Support:         ${enable_gnome}
    GIO Backend:           ${enable_gio} (gio-sharp >= $GIOSHARP_REQUIRED)
    GIO Hardware:          ${enable_gio_hardware} (gudev-sharp >= $GUDEVSHARP_REQUIRED)

  Clients:
    Beroe:                 ${enable_beroe}
    Halie:                 ${enable_halie}
    MediaPanel:            ${enable_mediapanel}
    Muinshee:              ${enable_muinshee}
    Nereid:                ${enable_nereid}

  DAP Support:
    Apple Device:          ${enable_appledevice} (libgpod-sharp)
    Karma:                 ${enable_karma} (libkarma-sharp)
    Mass Storage:          ${enable_mass_storage}
    MTP:                   ${enable_mtp} (libmtp)

  Extra Features:
    DAAP Support:          ${with_daap} (mono-zeroconf >= $MZC_REQUIRED)
    Web Browser:           ${with_webkit} (webkitgtk-3.0 >= $WEBKIT_MIN_VERSION, libsoup-2.4 >= $SOUP_MIN_VERSION)

  Extensions:
    AmazonMp3:             ${enable_amazonmp3}
    AmazonMp3.Store:       ${enable_amazonmp3_store}
    Audiobook:             ${enable_audiobook}
    Boo Scripting:         ${enable_booscript} (boo >= $BOO_REQUIRED)
    Bpm:                   ${enable_bpm}
    CoverArt:              ${enable_coverart}
    DAAP:                  ${enable_daap}
    Emusic:                ${enable_emusic}
    Emusic.Store:          ${enable_emusic_store}
    FileSystemQueue:       ${enable_filesystemqueue}
    Fixup:                 ${enable_fixup}
    InternetArchive:       ${enable_internetarchive}
    InternetRadio:         ${enable_internetradio}
    Lastfm:                ${enable_lastfm}
    LastfmStreaming:       ${enable_lastfmstreaming}
    LibraryWatcher:        ${enable_librarywatcher}
    MediaPanel:            ${enable_mediapanel}
    MiniMode:              ${enable_minimode}
    MiroGuide:             ${enable_miroguide}
    Mpris:                 ${enable_mpris}
    MultimediaKeys:        ${enable_multimediakeys}
    NotificationArea:      ${enable_notificationarea}
    NowPlaying:            ${enable_nowplaying}
    OpticalDisc:           ${enable_opticaldisc}
    PlayerMigration:       ${enable_playermigration}
    PlayQueue:             ${enable_playqueue}
    Podcasting:            ${enable_podcasting}
    RemoteAudio:           ${enable_remoteaudio}
    Sample:                ${enable_sample}
    Sound Menu:            ${enable_soundmenu}
    SqlDebugConsole:       ${enable_sqldebugconsole}
    Torrent:               ${enable_torrent} (monotorrent-dbus >= $MTD_VERSION)
    Ubuntu One Store:      ${enable_ubuntuone} (ubuntuone-sharp >= $UBUNTUONESHARP_REQUIRED)
    UPnP:                  ${enable_upnp} (mono-upnp >= $MONOUPNP_REQUIRED)
    Wikipedia:             ${enable_wikipedia}
    YouTube:               ${enable_youtube} (gdata-sharp >= $GDATASHARP_REQUIRED_VERSION)

  Build/Development:
    Unit Tests:            ${do_tests} (nunit >= ${NUNIT_REQUIRED})
    API Docs:              ${enable_docs} (monodocer and mdassembler)
    User Help:             ${enable_user_help} (gnome-doc-utils >= 0.17.3)
    Release Build:         ${enable_release}
    Vendor Build ID:       ${vendor_build_id}
])

AS_IF([test -d ${expanded_libdir}/${PACKAGE}],
[
	AC_MSG_WARN([An existing Banshee install is in ${expanded_libdir}/${PACKAGE}])
])
