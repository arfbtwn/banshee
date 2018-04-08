//
// GtkBaseClient.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.IO;
using System.Runtime.InteropServices;

using Mono.Addins;

using Hyena;

using Banshee.Base;
using Banshee.Metrics;
using Banshee.Database;
using Banshee.ServiceStack;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public abstract class GtkBaseClient : Client
    {
        static GtkBaseClient () {
            Application.InitializePaths ();
            user_css = Path.Combine (Paths.ApplicationData, "gtk.css");
        }

        private static Type client_type;

        private static string user_css;

        private Gtk.CssProvider custom_provider;

        public static void Startup<T> (string [] args) where T : GtkBaseClient
        {
            Hyena.Log.InformationFormat ("Running Banshee {0}: [{1}]", Application.Version,
                Application.BuildDisplayInfo);

            // Boot the client
            Banshee.Gui.GtkBaseClient.Startup<T> ();
        }

        public static void Startup<T> () where T : GtkBaseClient
        {
            if (client_type != null) {
                throw new ApplicationException ("Only a single GtkBaseClient can be initialized through Entry<T>");
            }

            client_type = typeof (T);
            Hyena.Gui.CleanRoomStartup.Startup (Startup);
        }

        private static void Startup ()
        {
            ((GtkBaseClient)Activator.CreateInstance (client_type)).Run ();
        }

        private string default_icon_name;

        protected GtkBaseClient () : this (true, Application.IconName)
        {
        }

        protected GtkBaseClient (bool initializeDefault, string defaultIconName)
        {
            this.default_icon_name = defaultIconName;
            if (initializeDefault) {
                Initialize (true);
            }
        }

        protected virtual void PreInitializeGtk ()
        {
        }

        [DllImport ("libdbus-glib-1-2.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void dbus_g_thread_init ();

        protected virtual void InitializeGtk ()
        {
            Log.Debug ("Initializing GTK");

            Gtk.Application.Init ();

            // This could go into GtkBaseClient, but it's probably something we
            // should really only support at each client level
            if (File.Exists (user_css) && !ApplicationContext.CommandLine.Contains ("no-gtkcss")) {
                custom_provider = new Gtk.CssProvider ();
                custom_provider.LoadFromPath (user_css);
                Gtk.StyleContext.AddProviderForScreen (Gdk.Screen.Default, (Gtk.IStyleProvider)custom_provider,
                                                       600 /* GTK_STYLE_PROVIDER_PRIORITY_APPLICATION*/ );
            }

            if (ApplicationContext.CommandLine.Contains ("debug-gtkcss")) {
                Log.Information ("Note: gtk.css file will be checked for reload every 5 seconds!");
                GLib.Timeout.Add (5000, delegate {
                    if (custom_provider != null) {
                        custom_provider.LoadFromPath (user_css);
                        Gtk.StyleContext.ResetWidgets (Gdk.Screen.Default);
                        Log.Information ("gtk.css has been reloaded");
                    }
                    return true;
                });
            }
        }

        protected virtual void PostInitializeGtk ()
        {
            Log.Debug ("Post-Initializing GTK");
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes
                ("/Banshee/ThickClient/GtkBaseClient/PostInitializeGtk")) {
                try {
                    node.CreateInstance ();
                } catch (Exception e) {
                    Log.Error ("PostInitializeGtk extension failed to run", e);
                }
            }
        }

        protected void Initialize (bool registerCommonServices)
        {
            // Set the process name so system process listings and commands are pretty
            ApplicationContext.TrySetProcessName (Application.InternalName);

            PreInitializeGtk ();
            InitializeGtk ();
            Application.Initialize ();
            PostInitializeGtk ();

            Gtk.Window.DefaultIconName = default_icon_name;

            ThreadAssist.InitializeMainThread ();
            ThreadAssist.ProxyToMainHandler = Banshee.ServiceStack.Application.Invoke;

            Gdk.Global.ProgramClass = Application.InternalName;
            GLib.Global.ApplicationName = "Banshee";

            // TODO: Set this to "video" when we're playing a video. PulseAudio doesn't treat it differently
            // than "music" for now, but it would be more correct.
            Environment.SetEnvironmentVariable ("PULSE_PROP_media.role", "music");

            if (ApplicationContext.Debugging) {
                GLib.Log.SetLogHandler ("Gtk", GLib.LogLevelFlags.Critical, GLib.Log.PrintTraceLogFunction);
                Gdk.Window.DebugUpdates = !String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("GDK_DEBUG_UPDATES"));
            }

            ServiceManager.ServiceStarted += OnServiceStarted;

            // Register specific services this client will care about
            if (registerCommonServices) {
                Banshee.Gui.CommonServices.Register ();
            }

            OnRegisterServices ();

            Application.ShutdownPromptHandler = OnShutdownPrompt;
            Application.TimeoutHandler = RunTimeout;
            Application.IdleHandler = RunIdle;
            Application.IdleTimeoutRemoveHandler = IdleTimeoutRemove;
            BansheeMetrics.Started += OnMetricsStarted;

            // Start the core boot process

            Application.PushClient (this);
            Application.Run ();

            if (!Banshee.Configuration.DefaultApplicationHelper.NeverAsk && Banshee.Configuration.DefaultApplicationHelper.HaveHelper) {
                Application.ClientStarted += delegate {
                    Banshee.Gui.Dialogs.DefaultApplicationHelperDialog.RunIfAppropriate ();
                };
            }

            Log.Notify += OnLogNotify;
        }

        private void OnMetricsStarted ()
        {
            var metrics = BansheeMetrics.Instance;
            var screen = Gdk.Screen.Default;

            metrics.Add ("Display/NScreens",    Gdk.Display.Default.NScreens);
            metrics.Add ("Screen/Height",       screen.Height);
            metrics.Add ("Screen/Width",        screen.Width);
            metrics.Add ("Screen/IsComposited", screen.IsComposited);
            metrics.Add ("Screen/NMonitors",    screen.NMonitors);
        }

        public virtual void Run ()
        {
            RunIdle (delegate { OnStarted (); return false; });
            Log.Debug ("Starting GTK main loop");
            Gtk.Application.Run ();
        }

        protected virtual void OnRegisterServices ()
        {
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is BansheeDbConnection) {
                ServiceManager.ServiceStarted -= OnServiceStarted;
                BansheeDbFormatMigrator migrator = ((BansheeDbConnection)args.Service).Migrator;
                if (migrator != null) {
                    migrator.Started += OnMigratorStarted;
                    migrator.Finished += OnMigratorFinished;
                }
            }
        }

        private void OnMigratorStarted (object o, EventArgs args)
        {
            BansheeDbFormatMigrator migrator = (BansheeDbFormatMigrator)o;
            new BansheeDbFormatMigratorMonitor (migrator);
        }

        private void OnMigratorFinished (object o, EventArgs args)
        {
            BansheeDbFormatMigrator migrator = (BansheeDbFormatMigrator)o;
            migrator.Started -= OnMigratorStarted;
            migrator.Finished -= OnMigratorFinished;
        }

        private void OnLogNotify (LogNotifyArgs args)
        {
            RunIdle (delegate {
                ShowLogCoreEntry (args.Entry);
                return false;
            });
        }

        private void ShowLogCoreEntry (LogEntry entry)
        {
            Gtk.Window window = null;
            Gtk.MessageType mtype;

            if (ServiceManager.Contains<GtkElementsService> ()) {
                window = ServiceManager.Get<GtkElementsService> ().PrimaryWindow;
            }

            switch (entry.Type) {
                case LogEntryType.Warning:
                    mtype = Gtk.MessageType.Warning;
                    break;
                case LogEntryType.Information:
                    mtype = Gtk.MessageType.Info;
                    break;
                case LogEntryType.Error:
                default:
                    mtype = Gtk.MessageType.Error;
                    break;
            }

            Hyena.Widgets.HigMessageDialog dialog = new Hyena.Widgets.HigMessageDialog (
                window, Gtk.DialogFlags.Modal, mtype, Gtk.ButtonsType.Close, entry.Message, entry.Details);

            dialog.Title = String.Empty;
            dialog.Run ();
            dialog.Destroy ();
        }

        private bool OnShutdownPrompt ()
        {
            ConfirmShutdownDialog dialog = new ConfirmShutdownDialog ();
            try {
                return dialog.Run () != Gtk.ResponseType.Cancel;
            } finally {
                dialog.Destroy ();
            }
        }

        protected uint RunTimeout (uint milliseconds, TimeoutHandler handler)
        {
            return GLib.Timeout.Add (milliseconds, delegate { return handler (); });
        }

        protected uint RunIdle (IdleHandler handler)
        {
            return GLib.Idle.Add (delegate { return handler (); });
        }

        protected bool IdleTimeoutRemove (uint id)
        {
            return GLib.Source.Remove (id);
        }

        bool _disposed;

        protected override void Dispose (bool disposing)
        {
            if (_disposed) return;

            _disposed = true;

            base.Dispose (disposing);

            if (disposing) {
                Gtk.Application.Quit ();
            }
        }
    }
}
