// 
// ProxyConfiguration.cs
// 
// Author:
//   Iain Lane <laney@ubuntu.com>
//   Ting Z Zhou <ting.z.zhou@intel.com>
//   Aaron Bockover <abockover@novell.com>
//   Bertrand Lorentz <bertrand.lorentz@gmail.com>
// 
// Copyright 2010 Iain Lane
// Copyright 2010 Intel Corp
// Copyright 2010 Novell, Inc.
// Copyright 2013 Bertrand Lorentz
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
using System.Net;

using GLib;

using Hyena;

namespace Banshee.GnomeBackend
{
    public class ProxyConfiguration : IDisposable
    {
        private const string PROXY = "org.gnome.system.proxy";
        private const string PROXY_MODE = "mode";
        private const string PROXY_AUTO_URL = "autoconfig-url";
        private const string HTTP_PROXY = "org.gnome.system.proxy.http";
        private const string PROXY_USE_PROXY = "enabled";
        private const string PROXY_USE_AUTH = "use-authentication";
        private const string PROXY_HOST = "host";
        private const string PROXY_PORT = "port";
        private const string PROXY_USER = "authentication-user";
        private const string PROXY_PASSWORD = "authentication-password";
        private const string PROXY_BYPASS_LIST = "ignore-hosts";

        private Settings settings;
        private Settings settings_http;
        private uint refresh_id;

        public ProxyConfiguration ()
        {
            settings = new Settings (PROXY);
            settings_http = new Settings (HTTP_PROXY);
            //settings.ChangeEvent += OnSettingsChange;
            //settings_http.ChangeEvent += OnSettingsChange;

            RefreshProxy ();
        }

        public void Dispose ()
        {
            if (settings != null) {
                //settings.ChangeEvent -= OnSettingsChange;
                //settings_http.ChangeEvent -= OnSettingsChange;
                settings = null;
            }
        }

        private void OnSettingsChange (object o, ChangeEventArgs args)
        {
            if (refresh_id > 0) {
                return;
            }

            // Wait 5 seconds before reloading the proxy, and block any
            // other notifications. This notification will be raised on
            // any minor change (e.g. htt->http->http:->http:/->http://)
            // to any of the GNOME proxy settings. Also, at any given
            // point in the modification of the settings, the state may
            // be invalid, so retain the previous good configuration.
            // TODO: Timeout still needed ?
            refresh_id = GLib.Timeout.Add (5000, RefreshProxy);
        }

        private bool RefreshProxy ()
        {
            Hyena.Log.Information ("Updating web proxy from GSettings");
            try {
                HttpWebRequest.DefaultWebProxy = GetProxyFromSettings ();
            } catch {
                Hyena.Log.Warning ("Not updating proxy settings. Invalid state");
            }

            refresh_id = 0;
            return false;
        }

        /*private T Get<T> (string @namespace, string key)
        {
            try {
                return (T)gconf_client.Get (@namespace == null
                    ? key
                    : @namespace + "/" + key);
            } catch {
                return default (T);
            }
        }*/

        private WebProxy GetProxyFromSettings ()
        {
            var proxy_mode = settings.GetString (PROXY_MODE);
            var proxy_auto_url = settings.GetString (PROXY_AUTO_URL);
            var use_proxy = settings_http.GetBoolean (PROXY_USE_PROXY);
            var use_auth = settings_http.GetBoolean (PROXY_USE_AUTH);
            var proxy_host = settings_http.GetString (PROXY_HOST);
            var proxy_port = settings_http.GetInt (PROXY_PORT);
            var proxy_user = settings_http.GetString (PROXY_USER);
            var proxy_password = settings_http.GetString (PROXY_PASSWORD);
            var proxy_bypass_list = settings.GetStrv (PROXY_BYPASS_LIST);

            if (!use_proxy || proxy_mode == "none" || String.IsNullOrEmpty (proxy_host)) {
                Hyena.Log.Debug ("Direct connection, no proxy in use");
                return null;
            }

            var proxy = new WebProxy ();

            if (proxy_mode == "auto") {
                if (!String.IsNullOrEmpty (proxy_auto_url)) {
                    proxy.Address = new Uri (proxy_auto_url);
                    Hyena.Log.Debug ("Automatic proxy connection", proxy.Address.AbsoluteUri);
                } else {
                    Hyena.Log.Warning ("Direct connection, no proxy in use. Proxy mode was 'auto' but no automatic configuration URL was found.");
                    return null;
                }
            } else {
                proxy.Address = new Uri (String.Format ("http://{0}:{1}", proxy_host, proxy_port));
                proxy.Credentials = use_auth
                    ? new NetworkCredential (proxy_user, proxy_password)
                    : null;
                Hyena.Log.Debug ("Manual proxy connection", proxy.Address.AbsoluteUri);
            }

            if (proxy_bypass_list == null) {
                return proxy;
            }

            foreach (var host in proxy_bypass_list) {
                if (host.Contains ("*.local")) {
                    proxy.BypassProxyOnLocal = true;
                    continue;
                }

                proxy.BypassArrayList.Add (String.Format ("http://{0}", host));
            }

            return proxy;
        }
    }
}
