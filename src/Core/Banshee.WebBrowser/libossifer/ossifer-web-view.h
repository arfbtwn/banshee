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
