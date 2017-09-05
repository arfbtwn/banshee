//
// ossifer-web-view.c
// 
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2010 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#include <config.h>
#include "ossifer-web-view.h"

G_DEFINE_TYPE (OssiferWebView, ossifer_web_view, WEBKIT_TYPE_WEB_VIEW);

typedef OssiferNavigationResponse (* OssiferWebViewMimeTypePolicyDecisionRequestedCallback)
    (OssiferWebView *ossifer, const gchar *mimetype);

typedef OssiferNavigationResponse (* OssiferWebViewNavigationPolicyDecisionRequestedCallback)
    (OssiferWebView *ossifer, const gchar *uri);

typedef gchar * (* OssiferWebViewDownloadRequestedCallback)
    (OssiferWebView *ossifer, const gchar *mimetype, const gchar *uri, const gchar *suggested_filename);

typedef gchar * (* OssiferWebViewResourceRequestStartingCallback)
    (OssiferWebView *ossifer, const gchar *uri);

typedef void (* OssiferWebViewDownloadStatusChanged)
    (OssiferWebView *ossifer, OssiferDownloadStatus status, const gchar *mimetype, const gchar *uri);

typedef void (* OssiferWebViewLoadStatusChanged)
    (OssiferWebView *ossifer, OssiferLoadStatus status);

typedef struct {
    OssiferWebViewMimeTypePolicyDecisionRequestedCallback mime_type_policy_decision_requested;
    OssiferWebViewNavigationPolicyDecisionRequestedCallback navigation_policy_decision_requested;
    OssiferWebViewDownloadRequestedCallback download_requested;
    OssiferWebViewResourceRequestStartingCallback resource_request_starting;
    OssiferWebViewLoadStatusChanged load_status_changed;
    OssiferWebViewDownloadStatusChanged download_status_changed;
} OssiferWebViewCallbacks;

struct OssiferWebViewPrivate {
    OssiferWebViewCallbacks callbacks;
    OssiferSecurityLevel level;
};

// ---------------------------------------------------------------------------
// OssiferWebView Internal Implementation
// ---------------------------------------------------------------------------

static gboolean
ossifer_web_view_decide_policy (WebKitWebView *web_view, WebKitPolicyDecision *decision, WebKitPolicyDecisionType type, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (web_view);
    
    if (ossifer->priv->callbacks.navigation_policy_decision_requested == NULL) {
        return FALSE;
    }
    
    switch (type) {
    case WEBKIT_POLICY_DECISION_TYPE_NAVIGATION_ACTION:
    {
        WebKitNavigationPolicyDecision *navigation_decision = WEBKIT_NAVIGATION_POLICY_DECISION (decision);
        WebKitNavigationAction *action = webkit_navigation_policy_decision_get_navigation_action (navigation_decision);
        WebKitURIRequest *request = webkit_navigation_action_get_request (action);
        
        const gchar * uri = webkit_uri_request_get_uri (request);
        switch ((gint)ossifer->priv->callbacks.navigation_policy_decision_requested (ossifer, uri)) {
        case (gint)OSSIFER_NAVIGATION_UNHANDLED /* Ossifer addition for 'unhandled' */:
            return FALSE;
        case (gint)OSSIFER_NAVIGATION_DOWNLOAD:
            webkit_policy_decision_download (decision);
            break;
        case (gint)OSSIFER_NAVIGATION_IGNORE:
            webkit_policy_decision_ignore (decision);
            break;
        case (gint)OSSIFER_NAVIGATION_ACCEPT:
        default:
            webkit_policy_decision_use (decision);
            break;
        }
        break;
    }
    case WEBKIT_POLICY_DECISION_TYPE_RESPONSE:
    {
        WebKitResponsePolicyDecision *response_decision = WEBKIT_RESPONSE_POLICY_DECISION (decision);
        WebKitURIResponse *response = webkit_response_policy_decision_get_response (response_decision);
        
        const gchar* mimetype = webkit_uri_response_get_mime_type (response);
        switch ((gint)ossifer->priv->callbacks.mime_type_policy_decision_requested (ossifer, mimetype)) {
        case (gint)OSSIFER_NAVIGATION_UNHANDLED /* Ossifer addition for 'unhandled' */:
            return FALSE;
        case (gint)OSSIFER_NAVIGATION_DOWNLOAD:
            webkit_policy_decision_download (decision);
            break;
        case (gint)OSSIFER_NAVIGATION_IGNORE:
            webkit_policy_decision_ignore (decision);
            break;
        case (gint)OSSIFER_NAVIGATION_ACCEPT:
        default:
            webkit_policy_decision_use (decision);
            break;
        }
        break;
    }
    case WEBKIT_POLICY_DECISION_TYPE_NEW_WINDOW_ACTION:
    {
        //WebKitNavigationPolicyDecision *navigation_decision = WEBKIT_NAVIGATION_POLICY_DECISION (decision);
        /* Make a policy decision here. */
        break;
    }
    default:
        /* Making no decision results in webkit_policy_decision_use(). */
        return FALSE;
    }
    return TRUE;
}

static gboolean
ossifer_download_decide_destination (WebKitDownload *download, gchar *suggested_filename, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (user_data);
    gchar *destination_uri;

    if (ossifer->priv->callbacks.download_requested == NULL ||
        (destination_uri = ossifer->priv->callbacks.download_requested (
            ossifer,
            webkit_uri_response_get_mime_type(webkit_download_get_response (download)),
            webkit_uri_request_get_uri (webkit_download_get_request (download)),
            suggested_filename)) == NULL) {
        return FALSE;
    }

    webkit_download_set_destination (download, destination_uri);

    g_free (destination_uri);

    return TRUE;
}

static void
ossifer_download_finished (WebKitDownload *download, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (user_data);
    
    if (ossifer->priv->callbacks.download_status_changed != NULL) {
        ossifer->priv->callbacks.download_status_changed (ossifer,
            OSSIFER_DOWNLOAD_FINISHED,
            webkit_uri_response_get_mime_type (webkit_download_get_response (download)),
            webkit_download_get_destination (download));
    }
}

static void
ossifer_download_failed (WebKitDownload *download, GError *error, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (user_data);
    
    if (ossifer->priv->callbacks.download_status_changed != NULL) {
        ossifer->priv->callbacks.download_status_changed (ossifer,
            OSSIFER_DOWNLOAD_ERROR,
            webkit_uri_response_get_mime_type (webkit_download_get_response (download)),
            webkit_download_get_destination (download));
    }
}

static void
ossifer_web_context_download_started (WebKitWebContext *context, WebKitDownload *download, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (user_data);
    
    if (ossifer->priv->callbacks.download_status_changed != NULL) {
        ossifer->priv->callbacks.download_status_changed (ossifer,
            OSSIFER_DOWNLOAD_STARTED,
            webkit_uri_response_get_mime_type (webkit_download_get_response (download)),
            webkit_download_get_destination (download));
    }
    
    g_signal_connect (download, "decide-destination",
        G_CALLBACK (ossifer_download_decide_destination), ossifer);
        
    g_signal_connect (download, "finished",
        G_CALLBACK (ossifer_download_finished), ossifer);
        
    g_signal_connect (download, "failed",
        G_CALLBACK (ossifer_download_failed), ossifer);
}

static void
ossifer_web_view_update_security_status (OssiferWebView *ossifer, const char *uri)
{
    OssiferSecurityLevel level = OSSIFER_SECURITY_IS_UNKNOWN;
    
    GTlsCertificate *certificate;
    GTlsCertificateFlags tls_errors;
    
    if (webkit_web_view_get_tls_info (WEBKIT_WEB_VIEW (ossifer), &certificate, &tls_errors)) {
        level = tls_errors == 0 ?
            OSSIFER_SECURITY_IS_SECURE : OSSIFER_SECURITY_IS_BROKEN;
    }
    
    ossifer->priv->level = level;
}

static void
ossifer_web_view_load_changed (WebKitWebView *web_view, WebKitLoadEvent load_event, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (web_view);
    
    OssiferLoadStatus status = OSSIFER_LOAD_UNKNOWN;
    
    switch (load_event) {
    case WEBKIT_LOAD_STARTED:
    case WEBKIT_LOAD_REDIRECTED:
        status = OSSIFER_LOAD_PROVISIONAL;
    break;
    case WEBKIT_LOAD_COMMITTED:
    {
        const char* uri;
        uri = webkit_web_view_get_uri (web_view);
        ossifer_web_view_update_security_status (ossifer, uri);
        
        status = OSSIFER_LOAD_COMMITTED;
    break;
    }
    case WEBKIT_LOAD_FINISHED:
        status = OSSIFER_LOAD_FINISHED;
    break;
    }

    if (ossifer->priv->callbacks.load_status_changed != NULL) {
        ossifer->priv->callbacks.load_status_changed (ossifer, status);
    }
}

static gboolean
ossifer_web_view_load_failed (WebKitWebView *web_view, WebKitLoadEvent load_event, gchar *failing_uri, GError *error, gpointer user_data)
{
    OssiferWebView *ossifer = OSSIFER_WEB_VIEW (web_view);
    
    if (ossifer->priv->callbacks.load_status_changed != NULL) {
        ossifer->priv->callbacks.load_status_changed (ossifer, OSSIFER_LOAD_FAILED);
    }
    
    return TRUE;
}

/*static GtkWidget *
ossifer_web_view_create_plugin_widget (WebKitWebView *web_view, gchar *mime_type,
    gchar *uri, GHashTable *param, gpointer user_data)
{
    // FIXME: this is just a useless stub, but could be used to provide
    // overriding plugins that hook directly into Banshee - e.g. provide
    // in-page controls that match the functionality of Amazon's MP3
    // preview Flash control.
    //
    // I'm opting not to do this now, because this requires setting
    // "enable-plugins" to TRUE, which causes all the plugins to be
    // loaded, which can introduce instability. There should be a fix
    // to avoid building the plugin registry at all in libwebkit.
    return NULL;
}*/

// ---------------------------------------------------------------------------
// OssiferWebView Class/Object Implementation
// ---------------------------------------------------------------------------

static void
ossifer_web_view_class_init (OssiferWebViewClass *klass)
{
    g_type_class_add_private (klass, sizeof (OssiferWebViewPrivate));
}

static void
ossifer_web_view_init (OssiferWebView *ossifer)
{
    WebKitWebContext *context = webkit_web_context_get_default();
    
    WebKitSettings *settings = webkit_settings_new_with_settings (
        "enable-plugins", FALSE,
        "enable-page-cache", TRUE,
        NULL);
    webkit_web_view_set_settings (WEBKIT_WEB_VIEW (ossifer), settings);
        
    ossifer->priv = G_TYPE_INSTANCE_GET_PRIVATE (ossifer, OSSIFER_TYPE_WEB_VIEW, OssiferWebViewPrivate);

    webkit_settings_set_enable_plugins (settings, FALSE);
    webkit_settings_set_enable_page_cache (settings, TRUE);
        
    g_signal_connect (ossifer, "decide-policy",
        G_CALLBACK (ossifer_web_view_decide_policy), NULL);
        
    g_signal_connect (context, "download-started",
        G_CALLBACK (ossifer_web_context_download_started), ossifer);

    g_signal_connect (ossifer, "load-changed",
        G_CALLBACK (ossifer_web_view_load_changed), NULL);
        
    g_signal_connect (ossifer, "load-failed",
        G_CALLBACK (ossifer_web_view_load_failed), NULL);
}

// ---------------------------------------------------------------------------
// OssiferWebView Public Instance API
// ---------------------------------------------------------------------------

void
ossifer_web_view_set_callbacks (OssiferWebView *ossifer, OssiferWebViewCallbacks callbacks)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    ossifer->priv->callbacks = callbacks;
}

void
ossifer_web_view_load_uri (OssiferWebView *ossifer, const gchar *uri)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    webkit_web_view_load_uri (WEBKIT_WEB_VIEW (ossifer), uri);
}

void
ossifer_web_view_load_html (OssiferWebView *ossifer, const gchar *content, const gchar *base_uri)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    webkit_web_view_load_html (WEBKIT_WEB_VIEW (ossifer), content, base_uri);
}

const gchar *
ossifer_web_view_get_uri (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), NULL);
    return webkit_web_view_get_uri (WEBKIT_WEB_VIEW (ossifer));
}

const gchar *
ossifer_web_view_get_title (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), NULL);
    return webkit_web_view_get_title (WEBKIT_WEB_VIEW (ossifer));
}

gboolean
ossifer_web_view_can_go_back (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), FALSE);
    return webkit_web_view_can_go_back (WEBKIT_WEB_VIEW (ossifer));
}

gboolean
ossifer_web_view_can_go_forward (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), FALSE);
    return webkit_web_view_can_go_forward (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_go_back (OssiferWebView *ossifer)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_go_back (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_go_forward (OssiferWebView *ossifer)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_go_forward (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_reload (OssiferWebView *ossifer)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_reload (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_set_zoom (OssiferWebView *ossifer, gfloat zoomLevel)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_set_zoom_level (WEBKIT_WEB_VIEW (ossifer), zoomLevel);
}

gfloat
ossifer_web_view_get_zoom (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), 1);
    return webkit_web_view_get_zoom_level (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_reload_bypass_cache (OssiferWebView *ossifer)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    return webkit_web_view_reload_bypass_cache (WEBKIT_WEB_VIEW (ossifer));
}

void
ossifer_web_view_execute_script (OssiferWebView *ossifer, const gchar *script)
{
    g_return_if_fail (OSSIFER_WEB_VIEW (ossifer));
    webkit_web_view_run_javascript (WEBKIT_WEB_VIEW (ossifer), script, NULL, NULL, NULL);
}

OssiferSecurityLevel
ossifer_web_view_get_security_level (OssiferWebView *ossifer)
{
    g_return_val_if_fail (OSSIFER_WEB_VIEW (ossifer), OSSIFER_SECURITY_IS_UNKNOWN);
    
    return ossifer->priv->level;
}
