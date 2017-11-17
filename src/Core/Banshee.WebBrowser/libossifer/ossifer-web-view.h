#ifndef OSSIFER_WEB_VIEW_H
#define OSSIFER_WEB_VIEW_H

#include <webkit2/webkit2.h>

G_BEGIN_DECLS

#define OSSIFER_TYPE_WEB_VIEW               (ossifer_web_view_get_type ())
#define OSSIFER_WEB_VIEW(obj)               (G_TYPE_CHECK_INSTANCE_CAST ((obj), OSSIFER_TYPE_WEB_VIEW, OssiferWebView))
#define OSSIFER_WEB_VIEW_CLASS(klass)       (G_TYPE_CHECK_CLASS_CAST ((klass), OSSIFER_TYPE_WEB_VIEW, OssiferWebView))
#define OSSIFER_IS_WEB_VIEW(obj)            (G_TYPE_CHECK_INSTANCE_TYPE ((obj), OSSIFER_TYPE_WEB_VIEW))
#define OSSIFER_IS_WEB_VIEW_CLASS(klass)    (G_TYPE_CHECK_CLASS_TYPE ((klass), OSSIFER_TYPE_WEB_VIEW))

typedef struct OssiferWebView OssiferWebView;
typedef struct OssiferWebViewClass OssiferWebViewClass;
typedef struct OssiferWebViewPrivate OssiferWebViewPrivate;

typedef enum
{
    OSSIFER_SECURITY_IS_UNKNOWN,
    OSSIFER_SECURITY_IS_INSECURE,
    OSSIFER_SECURITY_IS_BROKEN,
    OSSIFER_SECURITY_IS_SECURE
} OssiferSecurityLevel;

typedef enum
{
    OSSIFER_DOWNLOAD_ERROR = -1,
    OSSIFER_DOWNLOAD_CREATED = 0,
    OSSIFER_DOWNLOAD_STARTED,
    OSSIFER_DOWNLOAD_CANCELLED,
    OSSIFER_DOWNLOAD_FINISHED
} OssiferDownloadStatus;

typedef enum
{
    OSSIFER_LOAD_UNKNOWN,
    OSSIFER_LOAD_PROVISIONAL,
    OSSIFER_LOAD_COMMITTED,
    OSSIFER_LOAD_FINISHED,
    OSSIFER_LOAD_FIRST_VISUALLY_NON_EMPTY_LAYOUT,
    OSSIFER_LOAD_FAILED
} OssiferLoadStatus;

typedef enum
{
    OSSIFER_NAVIGATION_ACCEPT,
    OSSIFER_NAVIGATION_IGNORE,
    OSSIFER_NAVIGATION_DOWNLOAD,
    OSSIFER_NAVIGATION_UNHANDLED = 1000
} OssiferNavigationResponse;

struct OssiferWebView {
    WebKitWebView parent;
    OssiferWebViewPrivate *priv;
};

struct OssiferWebViewClass {
    WebKitWebViewClass parent_class;
};

GType ossifer_web_view_get_type ();

G_END_DECLS

#endif /* OSSIFER_WEB_VIEW_H */
