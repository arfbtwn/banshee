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
        private delegate OssiferNavigationResponse MimeTypePolicyDecisionRequestedCallback (IntPtr ossifer,
            IntPtr mimetype);

        private delegate OssiferNavigationResponse NavigationPolicyDecisionRequestedCallback (IntPtr ossifer,
            IntPtr uri);

        private delegate IntPtr DownloadRequestedCallback (IntPtr ossifer, IntPtr mimetype, IntPtr uri,
            IntPtr suggestedFilename);

        private delegate IntPtr ResourceRequestStartingCallback (IntPtr ossifer, IntPtr uri);

        private delegate void LoadStatusChangedCallback (IntPtr ossifer, OssiferLoadStatus status);

        private delegate void DownloadStatusChangedCallback (IntPtr ossifer, OssiferDownloadStatus status,
            IntPtr mimetype, IntPtr destnationUri);

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

        private const string Libossifer = "ossifer";

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Callbacks callbacks;

        public event EventHandler LoadStatusChanged;
        public event Action<float> ZoomChanged;

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_get_type ();

        public new static GLib.GType GType => new GLib.GType (ossifer_web_view_get_type ());

        protected OssiferWebView (IntPtr raw) : base (raw)
        {
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_set_callbacks (IntPtr ossifer, Callbacks callbacks);

        public OssiferWebView ()
        {
            OssiferSession.Initialize ();

            callbacks = new Callbacks ()
            {
                MimeTypePolicyDecisionRequested =
                    HandleMimeTypePolicyDecisionRequested,
                NavigationPolicyDecisionRequested =
                    HandleNavigationPolicyDecisionRequested,
                DownloadRequested = HandleDownloadRequested,
                ResourceRequestStarting = HandleResourceRequestStarting,
                LoadStatusChanged = HandleLoadStatusChanged,
                DownloadStatusChanged = HandleDownloadStatusChanged
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

        private IntPtr HandleDownloadRequested (IntPtr ossifer, IntPtr mimetype, IntPtr uri, IntPtr suggestedFilename)
        {
            var destination_uri = OnDownloadRequested (
                GLib.Marshaller.Utf8PtrToString (mimetype),
                GLib.Marshaller.Utf8PtrToString (uri),
                GLib.Marshaller.Utf8PtrToString (suggestedFilename));
            return destination_uri == null
                ? IntPtr.Zero
                : GLib.Marshaller.StringToPtrGStrdup (destination_uri);
        }

        protected virtual string OnDownloadRequested (string mimetype, string uri, string suggestedFilename)
        {
            return null;
        }

        private IntPtr HandleResourceRequestStarting (IntPtr ossifer, IntPtr oldUri)
        {
            string new_uri = OnResourceRequestStarting (GLib.Marshaller.Utf8PtrToString (oldUri));
            return new_uri == null
                ? IntPtr.Zero
                : GLib.Marshaller.StringToPtrGStrdup (new_uri);
        }

        protected virtual string OnResourceRequestStarting (string oldUri)
        {
            return null;
        }

        private void HandleLoadStatusChanged (IntPtr ossifer, OssiferLoadStatus status)
        {
            LoadStatus = status;
            OnLoadStatusChanged (status);
        }

        protected virtual void OnLoadStatusChanged (OssiferLoadStatus status)
        {
            var handler = LoadStatusChanged;
            handler?.Invoke (this, EventArgs.Empty);
        }

        private void HandleDownloadStatusChanged (IntPtr ossifer, OssiferDownloadStatus status, IntPtr mimetype,
            IntPtr destinationUri)
        {
            OnDownloadStatusChanged (status,
                GLib.Marshaller.Utf8PtrToString (mimetype),
                GLib.Marshaller.Utf8PtrToString (destinationUri));
        }

        protected virtual void OnDownloadStatusChanged (OssiferDownloadStatus status, string mimetype,
            string destinationUri)
        {
        }

        #endregion

        #region Public Instance API

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_load_uri (IntPtr ossifer, IntPtr uri);

        public void LoadUri (string uri)
        {
            var uri_raw = IntPtr.Zero;
            try {
                uri_raw = GLib.Marshaller.StringToPtrGStrdup (uri);
                ossifer_web_view_load_uri (Handle, uri_raw);
            }
            finally {
                GLib.Marshaller.Free (uri_raw);
            }
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_load_html (IntPtr ossifer, IntPtr content, IntPtr baseUri);

        public void LoadHtml (string content, string baseUri)
        {
            var content_raw = IntPtr.Zero;
            var base_uri_raw = IntPtr.Zero;

            try {
                ossifer_web_view_load_html (Handle,
                    content_raw = GLib.Marshaller.StringToPtrGStrdup (content),
                    base_uri_raw = GLib.Marshaller.StringToPtrGStrdup (baseUri));
            }
            finally {
                GLib.Marshaller.Free (content_raw);
                GLib.Marshaller.Free (base_uri_raw);
            }
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ossifer_web_view_can_go_forward (IntPtr ossifer);

        public virtual bool CanGoForward => ossifer_web_view_can_go_forward (Handle);

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ossifer_web_view_can_go_back (IntPtr ossifer);

        public virtual bool CanGoBack => ossifer_web_view_can_go_back (Handle);

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_go_forward (IntPtr ossifer);

        public virtual void GoForward ()
        {
            ossifer_web_view_go_forward (Handle);
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_go_back (IntPtr ossifer);

        public virtual void GoBack ()
        {
            ossifer_web_view_go_back (Handle);
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_set_zoom (IntPtr ossifer, float zoomLevel);

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern float ossifer_web_view_get_zoom (IntPtr ossifer);

        public float Zoom {
            get => ossifer_web_view_get_zoom (Handle);
            set {
                ossifer_web_view_set_zoom (Handle, value);
                var handler = ZoomChanged;
                handler?.Invoke (value);
            }
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_reload (IntPtr ossifer);

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_reload_bypass_cache (IntPtr ossifer);

        public virtual void Reload (bool bypassCache = false)
        {
            if (bypassCache) {
                ossifer_web_view_reload_bypass_cache (Handle);
            }
            else {
                ossifer_web_view_reload (Handle);
            }
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_stop_loading (IntPtr ossifer);

        public void StopLoading ()
        {
            ossifer_web_view_stop_loading (Handle);
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_execute_script (IntPtr ossifer, IntPtr script);

        public void ExecuteScript (string script)
        {
            var script_raw = IntPtr.Zero;
            try {
                ossifer_web_view_execute_script (Handle, script_raw = GLib.Marshaller.StringToPtrGStrdup (script));
            }
            finally {
                GLib.Marshaller.Free (script_raw);
            }
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_get_uri (IntPtr ossifer);

        public virtual string Uri => GLib.Marshaller.Utf8PtrToString (ossifer_web_view_get_uri (Handle));

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_get_title (IntPtr ossifer);

        public virtual string Title => GLib.Marshaller.Utf8PtrToString (ossifer_web_view_get_title (Handle));

        public virtual OssiferLoadStatus LoadStatus { get; private set; }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern OssiferSecurityLevel ossifer_web_view_get_security_level (IntPtr ossifer);

        public virtual OssiferSecurityLevel SecurityLevel => ossifer_web_view_get_security_level (Handle);

        #endregion
    }
}
