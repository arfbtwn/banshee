// 
// OssiferSession.cs
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
    public static class OssiferSession
    {
        private const string LIBOSSIFER = "ossifer";

        [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
        private delegate void CookieJarChangedCallback (IntPtr session, IntPtr old_cookie, IntPtr new_cookie);

        private static IntPtr handle;
        private static CookieJarChangedCallback cookie_jar_changed_callback;

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_session_initialize (IntPtr cookie_db_path,
            CookieJarChangedCallback cookie_jar_changed_callback);

        public static event Action<OssiferCookie, OssiferCookie> CookieChanged;

        public static void Initialize ()
        {
            /*if (handle != IntPtr.Zero) {
                return;
            }

            var path = System.IO.Path.Combine (Hyena.Paths.ApplicationData ?? ".", "ossifer-browser-cookies");
            var path_raw = IntPtr.Zero;
            try {
                cookie_jar_changed_callback = new CookieJarChangedCallback (HandleCookieJarChanged);
                handle = ossifer_session_initialize (path_raw = GLib.Marshaller.StringToPtrGStrdup (path),
                    cookie_jar_changed_callback);
            } finally {
                GLib.Marshaller.Free (path_raw);
            }*/
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_session_set_cookie (IntPtr name, IntPtr value,
            IntPtr domain, IntPtr path, int max_age);

        public static void SetCookie (string name, string value, string domain, string path, TimeSpan maxAge)
        {
            var name_raw = IntPtr.Zero;
            var value_raw = IntPtr.Zero;
            var domain_raw = IntPtr.Zero;
            var path_raw = IntPtr.Zero;

            try {
                /*ossifer_session_set_cookie (
                    name_raw = GLib.Marshaller.StringToPtrGStrdup (name),
                    value_raw = GLib.Marshaller.StringToPtrGStrdup (value),
                    domain_raw = GLib.Marshaller.StringToPtrGStrdup (domain),
                    path_raw = GLib.Marshaller.StringToPtrGStrdup (path),
                    (int)Math.Round (maxAge.TotalSeconds));*/
            } finally {
                GLib.Marshaller.Free (name_raw);
                GLib.Marshaller.Free (value_raw);
                GLib.Marshaller.Free (domain_raw);
                GLib.Marshaller.Free (path_raw);
            }
        }

        private static void HandleCookieJarChanged (IntPtr session, IntPtr old_cookie, IntPtr new_cookie)
        {
            OnCookieJarChanged (
                old_cookie == IntPtr.Zero ? null : new OssiferCookie (old_cookie),
                new_cookie == IntPtr.Zero ? null : new OssiferCookie (new_cookie));
        }

        private static void OnCookieJarChanged (OssiferCookie oldCookie, OssiferCookie newCookie)
        {
            var handler = CookieChanged;
            if (handler != null) {
                handler (oldCookie, newCookie);
            }
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_session_get_cookie (IntPtr name, IntPtr domain, IntPtr path);

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_cookie_free (IntPtr cookie);

        public static OssiferCookie GetCookie (string name, string domain, string path)
        {
            return null;
/*            var name_raw = IntPtr.Zero;
            var domain_raw = IntPtr.Zero;
            var path_raw = IntPtr.Zero;

            try {
                var ptr = ossifer_session_get_cookie (
                    name_raw = GLib.Marshaller.StringToPtrGStrdup (name),
                    domain_raw = GLib.Marshaller.StringToPtrGStrdup (domain),
                    path_raw = GLib.Marshaller.StringToPtrGStrdup (path));
                if (ptr != IntPtr.Zero) {
                    var cookie = new OssiferCookie (ptr);
                    ossifer_cookie_free (ptr);
                    return cookie;
                }
                return null;
            } finally {
                GLib.Marshaller.Free (name_raw);
                GLib.Marshaller.Free (domain_raw);
                GLib.Marshaller.Free (path_raw);
            }*/
        }

        [DllImport (LIBOSSIFER, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ossifer_session_delete_cookie (IntPtr name, IntPtr domain, IntPtr path);

        public static bool DeleteCookie (string name, string domain, string path)
        {
            return true;
/*            var name_raw = IntPtr.Zero;
            var domain_raw = IntPtr.Zero;
            var path_raw = IntPtr.Zero;

            try {
                return ossifer_session_delete_cookie (
                    name_raw = GLib.Marshaller.StringToPtrGStrdup (name),
                    domain_raw = GLib.Marshaller.StringToPtrGStrdup (domain),
                    path_raw = GLib.Marshaller.StringToPtrGStrdup (path));
            } finally {
                GLib.Marshaller.Free (name_raw);
                GLib.Marshaller.Free (domain_raw);
                GLib.Marshaller.Free (path_raw);
            }*/
        }
    }
}