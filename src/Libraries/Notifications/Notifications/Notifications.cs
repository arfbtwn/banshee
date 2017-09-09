/*
 * Copyright (c) 2006 Sebastian Dröge <slomo@circular-chaos.org>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.Collections.Generic;
using DBus;
using org.freedesktop.DBus;

namespace Notifications
{
    [Interface ("org.freedesktop.Notifications")]
    internal interface INotifications : Introspectable, Properties
    {
        ServerInformation GetServerInformation ();
        string[] GetCapabilities ();
        void CloseNotification (uint id);

        uint Notify (string app_name, uint id, string icon, string summary, string body,
            string[] actions, IDictionary<string, object> hints, int timeout);

        event NotificationClosedHandler NotificationClosed;
        event ActionInvokedHandler ActionInvoked;
    }

    public enum CloseReason : uint
    {
        Expired = 1,
        User = 2,
        API = 3,
        Reserved = 4
    }

    internal delegate void NotificationClosedHandler (uint id, uint reason);

    internal delegate void ActionInvokedHandler (uint id, string action);

    public struct ServerInformation
    {
        public string Name;
        public string Vendor;
        public string Version;
        public string SpecVersion;
    }

    public static class Global
    {
        private const string InterfaceName = "org.freedesktop.Notifications";
        private const string ObjectPath = "/org/freedesktop/Notifications";

        private static INotifications dbus_object;
        private static readonly object DbusObjectLock = new object ();

        internal static INotifications DBusObject {
            get {
                if (dbus_object != null)
                    return dbus_object;

                lock (DbusObjectLock) {
                    if (!Bus.Session.NameHasOwner (InterfaceName))
                        Bus.Session.StartServiceByName (InterfaceName);

                    dbus_object = Bus.Session.GetObject<INotifications>
                        (InterfaceName, new ObjectPath (ObjectPath));
                    return dbus_object;
                }
            }
        }

        public static string[] Capabilities => DBusObject.GetCapabilities ();

        public static ServerInformation ServerInformation => DBusObject.GetServerInformation ();
    }
}
