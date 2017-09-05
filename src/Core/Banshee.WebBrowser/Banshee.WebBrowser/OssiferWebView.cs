// 
// OssiferWebView.cs
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

using System;
using System.Runtime.InteropServices;

namespace Banshee.WebBrowser
{
    public class OssiferWebView : Gtk.Widget
    {
        private delegate OssiferNavigationResponse MimeTypePolicyDecisionRequestedCallback (IntPtr ossifer, IntPtr mimetype);
        private delegate OssiferNavigationResponse NavigationPolicyDecisionRequestedCallback (IntPtr ossifer, IntPtr uri);
        private delegate IntPtr DownloadRequestedCallback (IntPtr ossifer, IntPtr mimetype, IntPtr uri, IntPtr suggested_filename);
        private delegate IntPtr ResourceRequestStartingCallback (IntPtr ossifer, IntPtr uri);
        private delegate void LoadStatusChangedCallback (IntPtr ossifer, OssiferLoadStatus status);
        private delegate void DownloadStatusChangedCallback (IntPtr ossifer, OssiferDownloadStatus status, IntPtr mimetype, IntPtr destnation_uri);

        [StructLayout (LayoutKind.Sequential)]
        private struct Callbacks
        {
            public MimeTypePolicyDecisionRequestedCallback MimeTypePolicyDecisionRequested;
            public NavigationPolicyDecisionRequestedCallback NavigationPolicyDecisionRequested;
            public DownloadRequestedCallback DownloadRequested;
            public ResourceRequestStartingCallback ResourceRequestStarting;
            public LoadStatusChangedCallback LoadStatusChanged;
            public DownloadStatusChangedCallback DownloadStatusChanged;
        }

        private const string LIBOSSIFER = "ossifer";

        private Callbacks callbacks;

        public event EventHandler LoadStatusChanged;
        public event Action<float> ZoomChanged;

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_get_type ();

        public static new GLib.GType GType {
            get { return new GLib.GType (ossifer_web_view_get_type ()); }
        }

        protected OssiferWebView (IntPtr raw) : base (raw)
        {
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_set_callbacks (IntPtr ossifer, Callbacks callbacks);

        public OssiferWebView ()
        {
            OssiferSession.Initialize ();
            CreateNativeObject (new string[0], new GLib.Value[0]);

            callbacks = new Callbacks () {
                MimeTypePolicyDecisionRequested =
                    new MimeTypePolicyDecisionRequestedCallback (HandleMimeTypePolicyDecisionRequested),
                NavigationPolicyDecisionRequested =
                    new NavigationPolicyDecisionRequestedCallback (HandleNavigationPolicyDecisionRequested),
                DownloadRequested = new DownloadRequestedCallback (HandleDownloadRequested),
                ResourceRequestStarting = new ResourceRequestStartingCallback (HandleResourceRequestStarting),
                LoadStatusChanged = new LoadStatusChangedCallback (HandleLoadStatusChanged),
                DownloadStatusChanged = new DownloadStatusChangedCallback (HandleDownloadStatusChanged)
            };

            ossifer_web_view_set_callbacks (Handle, callbacks);
        }

#region Callback Implementations

        private OssiferNavigationResponse HandleMimeTypePolicyDecisionRequested (IntPtr ossifer, IntPtr mimetype)
        {
            return OnMimeTypePolicyDecisionRequested (GLib.Marshaller.Utf8PtrToString (mimetype));
        }

        protected virtual OssiferNavigationResponse OnMimeTypePolicyDecisionRequested (string mimetype)
        {
            return OssiferNavigationResponse.Unhandled;
        }

        private OssiferNavigationResponse HandleNavigationPolicyDecisionRequested (IntPtr ossifer, IntPtr uri)
        {
            return OnNavigationPolicyDecisionRequested (GLib.Marshaller.Utf8PtrToString (uri));
        }

        protected virtual OssiferNavigationResponse OnNavigationPolicyDecisionRequested (string uri)
        {
            return OssiferNavigationResponse.Unhandled;
        }

        private IntPtr HandleDownloadRequested (IntPtr ossifer, IntPtr mimetype, IntPtr uri, IntPtr suggested_filename)
        {
            var destination_uri = OnDownloadRequested (
                GLib.Marshaller.Utf8PtrToString (mimetype),
                GLib.Marshaller.Utf8PtrToString (uri),
                GLib.Marshaller.Utf8PtrToString (suggested_filename));
            return destination_uri == null
                ? IntPtr.Zero
                : GLib.Marshaller.StringToPtrGStrdup (destination_uri);
        }

        protected virtual string OnDownloadRequested (string mimetype, string uri, string suggestedFilename)
        {
            return null;
        }

        private IntPtr HandleResourceRequestStarting (IntPtr ossifer, IntPtr old_uri)
        {
            string new_uri = OnResourceRequestStarting (GLib.Marshaller.Utf8PtrToString (old_uri));
            return new_uri == null
                ? IntPtr.Zero
                : GLib.Marshaller.StringToPtrGStrdup (new_uri);
        }

        protected virtual string OnResourceRequestStarting (string old_uri)
        {
            return null;
        }

        private void HandleLoadStatusChanged (IntPtr ossifer, OssiferLoadStatus status)
        {
            OnLoadStatusChanged (status);
        }

        protected virtual void OnLoadStatusChanged (OssiferLoadStatus status)
        {
            var handler = LoadStatusChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void HandleDownloadStatusChanged (IntPtr ossifer, OssiferDownloadStatus status, IntPtr mimetype, IntPtr destinationUri)
        {
            OnDownloadStatusChanged (status,
                GLib.Marshaller.Utf8PtrToString (mimetype),
                GLib.Marshaller.Utf8PtrToString (destinationUri));
        }

        protected virtual void OnDownloadStatusChanged (OssiferDownloadStatus status, string mimetype, string destinationUri)
        {
        }

#endregion

#region Public Instance API

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_load_uri (IntPtr ossifer, IntPtr uri);

        public void LoadUri (string uri)
        {
            var uri_raw = IntPtr.Zero;
            try {
                uri_raw = GLib.Marshaller.StringToPtrGStrdup (uri);
                ossifer_web_view_load_uri (Handle, uri_raw);
            } finally {
                GLib.Marshaller.Free (uri_raw);
            }
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_load_string (IntPtr ossifer,
            IntPtr content, IntPtr mimetype, IntPtr encoding, IntPtr base_uri);

        public void LoadString (string content, string mimetype, string encoding, string baseUri)
        {
            var content_raw = IntPtr.Zero;
            var mimetype_raw = IntPtr.Zero;
            var encoding_raw = IntPtr.Zero;
            var base_uri_raw = IntPtr.Zero;

            try {
                ossifer_web_view_load_string (Handle,
                    content_raw = GLib.Marshaller.StringToPtrGStrdup (content),
                    mimetype_raw = GLib.Marshaller.StringToPtrGStrdup (mimetype),
                    encoding_raw = GLib.Marshaller.StringToPtrGStrdup (encoding),
                    base_uri_raw = GLib.Marshaller.StringToPtrGStrdup (baseUri));
            } finally {
                GLib.Marshaller.Free (content_raw);
                GLib.Marshaller.Free (mimetype_raw);
                GLib.Marshaller.Free (encoding_raw);
                GLib.Marshaller.Free (base_uri_raw);
            }
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ossifer_web_view_can_go_forward (IntPtr ossifer);

        public virtual bool CanGoForward {
            get { return ossifer_web_view_can_go_forward (Handle); }
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ossifer_web_view_can_go_back (IntPtr ossifer);

        public virtual bool CanGoBack {
            get { return ossifer_web_view_can_go_back (Handle); }
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_go_forward (IntPtr ossifer);

        public virtual void GoForward ()
        {
            ossifer_web_view_go_forward (Handle);
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_go_back (IntPtr ossifer);

        public virtual void GoBack ()
        {
            ossifer_web_view_go_back (Handle);
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_set_zoom (IntPtr ossifer, float zoomLevel);

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern float ossifer_web_view_get_zoom (IntPtr ossifer);

        public float Zoom {
            get { return ossifer_web_view_get_zoom (Handle); }
            set {
                ossifer_web_view_set_zoom (Handle, value);
                var handler = ZoomChanged;
                if (handler != null) {
                    handler (value);
                }
            }
        }

        const float ZoomStep = 0.05f;

        public void ZoomIn ()
        {
            Zoom += ZoomStep;
        }

        public void ZoomOut ()
        {
            Zoom -= ZoomStep;
        }

        protected override bool OnScrollEvent (Gdk.EventScroll scroll)
        {
            //TODO: Handle smooth scroll events when gtk-sharp exposes deltaX/Y fields.
            //      See https://github.com/arfbtwn/banshee/issues/37
            /*if ((scroll.State & Gdk.ModifierType.ControlMask) != 0) {
                Zoom += (scroll.Direction == Gdk.ScrollDirection.Up) ? ZOOM_STEP : -ZOOM_STEP;
                return true;
            }*/

            return base.OnScrollEvent (scroll);
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_reload (IntPtr ossifer);

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_reload_bypass_cache (IntPtr ossifer);

        public virtual void Reload (bool bypassCache)
        {
            if (bypassCache) {
                ossifer_web_view_reload_bypass_cache (Handle);
            } else {
                ossifer_web_view_reload (Handle);
            }
        }

        public void Reload ()
        {
            Reload (false);
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_execute_script (IntPtr ossifer, IntPtr script);

        public void ExecuteScript (string script)
        {
            var script_raw = IntPtr.Zero;
            try {
                ossifer_web_view_execute_script (Handle, script_raw = GLib.Marshaller.StringToPtrGStrdup (script));
            } finally {
                GLib.Marshaller.Free (script_raw);
            }
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_get_uri (IntPtr ossifer);

        public virtual string Uri {
            get { return GLib.Marshaller.Utf8PtrToString (ossifer_web_view_get_uri (Handle)); }
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_get_title (IntPtr ossifer);

        public virtual string Title {
            get { return GLib.Marshaller.Utf8PtrToString (ossifer_web_view_get_title (Handle)); }
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern OssiferLoadStatus ossifer_web_view_get_load_status (IntPtr ossifer);

        public virtual OssiferLoadStatus LoadStatus {
            get { return ossifer_web_view_get_load_status (Handle); }
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern OssiferSecurityLevel ossifer_web_view_get_security_level (IntPtr ossifer);

        public virtual OssiferSecurityLevel SecurityLevel {
            get { return ossifer_web_view_get_security_level (Handle); }
        }

#endregion

    }
}
