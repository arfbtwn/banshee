SUBDIRS = libossifer

ASSEMBLY = Banshee.WebBrowser
TARGET = library
LINK = $(REF_BANSHEE_WEBBROWSER)
INSTALL_DIR = $(DEFAULT_INSTALL_DIR)

SOURCES =  \
	Banshee.WebBrowser/NavigationControl.cs \
	Banshee.WebBrowser/OssiferCookie.cs \
	Banshee.WebBrowser/OssiferDownloadStatus.cs \
	Banshee.WebBrowser/OssiferLoadStatus.cs \
	Banshee.WebBrowser/OssiferNavigationResponse.cs \
	Banshee.WebBrowser/OssiferSecurityLevel.cs \
	Banshee.WebBrowser/OssiferSession.cs \
	Banshee.WebBrowser/OssiferWebView.cs \
	Banshee.WebSource/WebBrowserShell.cs \
	Banshee.WebSource/WebSource.cs \
	Banshee.WebSource/WebView.cs \
	JavaScriptCore.Bridge/Runtime.cs \
	JavaScriptCore.Bridge/Tests/RuntimeTests.cs \
	JavaScriptCore/JSClass.cs \
	JavaScriptCore/JSClassAttribute.cs \
	JavaScriptCore/JSClassDefinition.cs \
	JavaScriptCore/JSContext.cs \
	JavaScriptCore/JSError.cs \
	JavaScriptCore/JSErrorException.cs \
	JavaScriptCore/JSException.cs \
	JavaScriptCore/JSFunction.cs \
	JavaScriptCore/JSObject.cs \
	JavaScriptCore/JSPropertyAttribute.cs \
	JavaScriptCore/JSPropertyNameAccumulator.cs \
	JavaScriptCore/JSPropertyNameArray.cs \
	JavaScriptCore/JSStaticFunction.cs \
	JavaScriptCore/JSStaticFunctionAttribute.cs \
	JavaScriptCore/JSString.cs \
	JavaScriptCore/JSType.cs \
	JavaScriptCore/JSValue.cs \
	JavaScriptCore/ManagedPropertyBagClass.cs \
	JavaScriptCore/Tests/JSClassTests.cs \
	JavaScriptCore/Tests/JSFunctionTests.cs \
	JavaScriptCore/Tests/JSObjectTests.cs \
	JavaScriptCore/Tests/JSStringTests.cs \
	JavaScriptCore/Tests/JSValueTests.cs

RESOURCES =  \
	Resources/loading.html

if HAVE_LIBWEBKIT
include $(top_srcdir)/build/build.mk
module_SCRIPTS += Banshee.WebBrowser.dll.config
else
include $(top_srcdir)/build/build.dist.mk
endif

EXTRA_DIST += Banshee.WebBrowser.dll.config
