//
// MultimediaKeyService.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Alexander Hixon <hixon.alexander@mediati.org>
//   Jan Arne Petersen <jap@gnome.org>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Mono.Unix;

using DBus;

using Hyena;
using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.MultimediaKeys
{
    public class MultimediaKeysService : IExtensionService, IDisposable
    {
        private const string BusName = "org.gnome.SettingsDaemon";
        private const string ObjectPath = "/org/gnome/SettingsDaemon";

        internal delegate void MediaPlayerKeyPressedHandler (string application, string key);

        // GNOME 2.20
        [Interface ("org.gnome.SettingsDaemon")]
        internal interface ISettingsDaemon220
        {
            void GrabMediaPlayerKeys (string application, uint time);
            void ReleaseMediaPlayerKeys (string application);
            event MediaPlayerKeyPressedHandler MediaPlayerKeyPressed;
        }

        // GNOME 2.22
        [Interface ("org.gnome.SettingsDaemon.MediaKeys")]
        internal interface ISettingsDaemon222
        {
            void GrabMediaPlayerKeys (string application, uint time);
            void ReleaseMediaPlayerKeys (string application);
            event MediaPlayerKeyPressedHandler MediaPlayerKeyPressed;
        }

        private ISettingsDaemon222 settings_daemon_222;
        private ISettingsDaemon220 settings_daemon_220;

        private readonly string app_name;

        public MultimediaKeysService ()
        {
            app_name = Application.InternalName;
            if (ApplicationContext.Debugging) {
                app_name += "-debug";
            }
        }

        void IExtensionService.Initialize ()
        {
            // It's a common theme in GNOME for APIs to change every 6 months
            // We are essentially an ISV, and actually care about backwards
            // compatibility! Therefore, fall back to the interface for GNOME 2.20
            // if the GNOME 2.22 interface cannot be found.
            //
            // Additionally, there is a design limitation in Managed DBus that restricts
            // it from flattening/following managed interfaces, therefore we must
            // explicitly handle two different GSD objects as if they had nothing in
            // common at this point. This at least will be fixed in Managed DBus.
            //
            // For instance, we can't do this currently (semi-pseudo-code):
            //
            //    interface IGsdBase { void Food (); }
            //    [Interface ("org.gnome.SettingsDaemon")] interface IGsd220 : IGsdBase { }
            //    [Interface ("org.gnome.SettingsDaemon.MediaKeys")] interface IGsd222 : IGsdBase { }
            //
            //    IGsdBase gsd = bus.GetObject<IGsd222> (name, path_222)
            //    if (gsd == null)
            //        gsd = bus.GetObject<IGsd220> (name, path_220)
            //    gsd.Foo ();
            //

            try {
                settings_daemon_222 = Bus.Session.GetObject<ISettingsDaemon222> (BusName,
                    new ObjectPath (ObjectPath + "/MediaKeys"));
                settings_daemon_222.GrabMediaPlayerKeys (app_name, 0);
                settings_daemon_222.MediaPlayerKeyPressed += OnMediaPlayerKeyPressed;

                Log.Debug ("Using GNOME 2.22 API for Multimedia Keys");
            } catch {
                settings_daemon_222 = null;

                try {
                    settings_daemon_220 = Bus.Session.GetObject<ISettingsDaemon220> (BusName,
                        new ObjectPath (ObjectPath));
                    settings_daemon_220.GrabMediaPlayerKeys (app_name, 0);
                    settings_daemon_220.MediaPlayerKeyPressed += OnMediaPlayerKeyPressed;

                    Log.Debug ("Using GNOME 2.20 API for Multimedia keys");
                } catch {
                    settings_daemon_220 = null;
                    throw new ApplicationException ("No support GNOME Settings Daemon could be reached.");
                }
            }
        }

        public void Dispose ()
        {
            if (settings_daemon_222 != null) {
                settings_daemon_222.MediaPlayerKeyPressed -= OnMediaPlayerKeyPressed;
                settings_daemon_222.ReleaseMediaPlayerKeys (app_name);
                settings_daemon_222 = null;
            }

            if (settings_daemon_220 != null) {
                settings_daemon_220.MediaPlayerKeyPressed -= OnMediaPlayerKeyPressed;
                settings_daemon_220.ReleaseMediaPlayerKeys (app_name);
                settings_daemon_220 = null;
            }
        }

        private void OnMediaPlayerKeyPressed (string application, string key)
        {
            if (application != app_name) {
                return;
            }

            switch (key) {
                case "Play":
                case "Pause":
                    ServiceManager.PlayerEngine.TogglePlaying ();
                    break;
                case "Next":
                    ServiceManager.PlaybackController.Next ();
                    break;
                case "Previous":
                    ServiceManager.PlaybackController.RestartOrPrevious ();
                    break;
                case "Stop":
                    ServiceManager.PlayerEngine.Close ();
                    break;
                case "Repeat":
                    ServiceManager.PlaybackController.ToggleRepeat ();
                    break;
                case "Shuffle":
                    ServiceManager.PlaybackController.ToggleShuffle ();
                    break;
                case "FastForward":
                    if (ServiceManager.PlayerEngine.CanSeek) {
                        ServiceManager.PlayerEngine.Position += 20*1000;
                    }
                    break;
                case "Rewind":
                    if (ServiceManager.PlayerEngine.CanSeek) {
                        ServiceManager.PlayerEngine.Position -= 10*1000;
                    }
                    break;
            }
        }

        string IService.ServiceName {
            get { return "MultimediaKeysService"; }
        }
    }
}
