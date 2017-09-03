#include <config.h>
#include <webkit2/webkit2.h>

typedef struct OssiferSession OssiferSession;

typedef void (* OssiferSessionCookieJarChanged)
    (OssiferSession *session, SoupCookie *old_cookie, SoupCookie *new_cookie);

struct OssiferSession {
    OssiferSessionCookieJarChanged cookie_jar_changed;
};

static void
ossifer_session_cookie_jar_changed (SoupCookieJar *jar,
    SoupCookie *old_cookie, SoupCookie *new_cookie, gpointer user_data)
{
    OssiferSession *session = (OssiferSession *)user_data;
    if (session->cookie_jar_changed != NULL) {
        session->cookie_jar_changed (session, old_cookie, new_cookie);
    }
}

static SoupCookieJar *
ossifer_session_get_cookie_jar ()
{
    return (SoupCookieJar *)soup_session_get_feature (webkit_get_default_session (),
        SOUP_TYPE_COOKIE_JAR);
}

OssiferSession *
ossifer_session_initialize (const gchar *cookie_db_path,
    OssiferSessionCookieJarChanged cookie_jar_changed_callback)
{
    static OssiferSession *session_instance = NULL;

    SoupSession *session;
    SoupCookieJar *cookie_jar;
    gchar *path;

    if (session_instance != NULL) {
        return session_instance;
    }

    session_instance = g_new0 (OssiferSession, 1);
    session_instance->cookie_jar_changed = cookie_jar_changed_callback;

    session = webkit_get_default_session ();

    g_object_set (session,
                  SOUP_SESSION_SSL_USE_SYSTEM_CA_FILE, TRUE,
                  NULL);

    path = g_strdup_printf ("%s.sqlite", cookie_db_path);
    cookie_jar = soup_cookie_jar_db_new (path, FALSE);
    soup_session_add_feature (session, SOUP_SESSION_FEATURE (cookie_jar));
    g_object_unref (cookie_jar);
    g_free (path);

    g_signal_connect (cookie_jar, "changed",
        G_CALLBACK (ossifer_session_cookie_jar_changed),
        session_instance);

    return session_instance;
}

void
ossifer_session_set_cookie (const gchar *name, const gchar *value,
    const gchar *domain, const gchar *path, gint max_age)
{
    SoupCookie *cookie;
    SoupCookieJar *cookie_jar = ossifer_session_get_cookie_jar ();

    g_return_if_fail (cookie_jar != NULL);

    cookie = soup_cookie_new (name, value, domain, path, max_age);
    soup_cookie_jar_add_cookie (cookie_jar, cookie);
}

SoupCookie *
ossifer_session_get_cookie (const gchar *name, const gchar *domain, const gchar *path)
{
    GSList *cookies;
    GSList *item;
    SoupCookie *found_cookie = NULL;

    cookies = soup_cookie_jar_all_cookies (ossifer_session_get_cookie_jar ());

    for (item = cookies; item != NULL; item = item->next) {
        SoupCookie *cookie = (SoupCookie *)item->data;

        if (g_str_equal (name, cookie->name) &&
            g_str_equal (domain, cookie->domain) &&
            g_str_equal (path, cookie->path)) {
            found_cookie = soup_cookie_copy (cookie);
            break;
        }
    }

    soup_cookies_free (cookies);

    return found_cookie;
}

gboolean
ossifer_session_delete_cookie (const gchar *name, const gchar *domain, const gchar *path)
{
    SoupCookie *cookie = ossifer_session_get_cookie (name, domain, path);

    if (cookie != NULL) {
        soup_cookie_jar_delete_cookie (ossifer_session_get_cookie_jar (), cookie);
        soup_cookie_free (cookie);
        return TRUE;
    }

    return FALSE;
}

void
ossifer_cookie_free (SoupCookie *cookie)
{
    soup_cookie_free (cookie);
}
