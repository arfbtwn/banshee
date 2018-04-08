
//
// Application.cs
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
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Mono.Unix;

using Hyena;

using Banshee.Library;
using Banshee.Playlist;
using Banshee.SmartPlaylist;
using Banshee.Sources;
using Banshee.Base;

namespace Banshee.ServiceStack
{
    public delegate bool ShutdownRequestHandler ();
    public delegate bool TimeoutHandler ();
    public delegate bool IdleHandler ();
    public delegate bool IdleTimeoutRemoveHandler (uint id);
    public delegate uint TimeoutImplementationHandler (uint milliseconds, TimeoutHandler handler);
    public delegate uint IdleImplementationHandler (IdleHandler handler);
    public delegate bool IdleTimeoutRemoveImplementationHandler (uint id);

    public static class Application
    {
        private const string LibGlibLibrary = "libglib-2.0-0.dll";

        public static event ShutdownRequestHandler ShutdownRequested;
        public static event Action<Client> ClientAdded;

        private static event Action<Client> client_started;
        public static event Action<Client> ClientStarted {
            add {
                lock (running_clients) {
                    foreach (Client client in running_clients) {
                        if (client.IsStarted) {
                            OnClientStarted (client);
                        }
                    }
                }
                client_started += value;
            }
            remove { client_started -= value; }
        }

        private static Stack<Client> running_clients = new Stack<Client> ();
        private static bool shutting_down;

        public static void Initialize ()
        {
            ServiceManager.DefaultInitialize ();
        }

#if WIN32
        [DllImport ("msvcrt.dll", CallingConvention = CallingConvention.Cdecl) /* willfully unmapped */]
        public static extern int _putenv (string varName);
#endif

        public static void Run ()
        {
#if WIN32
            // There are two sets of environement variables we need to impact with our LANG.
            // refer to : http://article.gmane.org/gmane.comp.gnu.mingw.user/8272
            var lang_code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            string env = String.Concat ("LANG=", lang_code);
            Environment.SetEnvironmentVariable ("LANG", lang_code);
            _putenv (env);
#endif

            Catalog.Init (Application.InternalName, System.IO.Path.Combine (
                Hyena.Paths.InstalledApplicationDataRoot, "locale"));

            DBusConnection.Init ();

            ServiceManager.Run ();

            ServiceManager.SourceManager.AddSource (new MusicLibrarySource (), true);
            ServiceManager.SourceManager.AddSource (new VideoLibrarySource (), false);
            ServiceManager.SourceManager.LoadExtensionSources ();
        }

        public static bool ShuttingDown {
            get { return shutting_down; }
        }

        public static void Shutdown ()
        {
            shutting_down = true;
            if (Banshee.Kernel.Scheduler.IsScheduled (typeof (Banshee.Kernel.IInstanceCriticalJob)) ||
                ServiceManager.JobScheduler.HasAnyDataLossJobs ||
                Banshee.Kernel.Scheduler.CurrentJob is Banshee.Kernel.IInstanceCriticalJob) {
                if (shutdown_prompt_handler != null && !shutdown_prompt_handler ()) {
                    shutting_down = false;
                    return;
                }
            }

            if (OnShutdownRequested ()) {
                Dispose ();
            }
            shutting_down = false;
        }

        public static void PushClient (Client client)
        {
            lock (running_clients) {
                running_clients.Push (client);
                client.Started += OnClientStarted;
            }

            Action<Client> handler = ClientAdded;
            if (handler != null) {
                handler (client);
            }
        }

        public static Client PopClient ()
        {
            lock (running_clients) {
                return running_clients.Pop ();
            }
        }

        public static Client ActiveClient {
            get { lock (running_clients) { return running_clients.Peek (); } }
        }

        private static void OnClientStarted (Client client)
        {
            client.Started -= OnClientStarted;
            Action<Client> handler = client_started;
            if (handler != null) {
                handler (client);
            }
        }

        static bool paths_initialized;
        public static void InitializePaths ()
        {
            if (!paths_initialized) {
                // We changed banshee-1 to banshee everywhere except the
                // ~/.config/banshee-1/ and ~/.cache/banshee-1 directories
                Paths.UserApplicationName = "banshee-1";
                Paths.ApplicationName = InternalName;
                paths_initialized = true;
            }
        }

        [DllImport (LibGlibLibrary, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr g_get_language_names ();

        public static void DisplayHelp (string page)
        {
            DisplayHelp ("banshee", page);
        }

        private static void DisplayHelp (string project, string page)
        {
            bool shown = false;

            foreach (var lang in GLib.Marshaller.NullTermPtrToStringArray (g_get_language_names (), false)) {
                string path = String.Format ("{0}/gnome/help/{1}/{2}",
                    Paths.InstalledApplicationDataRoot, project, lang);

                if (System.IO.Directory.Exists (path)) {
                    shown = Banshee.Web.Browser.Open (String.Format ("ghelp:{0}", path), false);
                    break;
                }
            }

            if (!shown) {
                Banshee.Web.Browser.Open (String.Format ("http://library.gnome.org/users/{0}/", project));
            }
        }

        private static bool OnShutdownRequested ()
        {
            ShutdownRequestHandler handler = ShutdownRequested;
            if (handler != null) {
                foreach (ShutdownRequestHandler del in handler.GetInvocationList ()) {
                    if (!del ()) {
                        return false;
                    }
                }
            }

            return true;
        }

        public static void Invoke (InvokeHandler handler)
        {
            RunIdle (delegate { handler (); return false; });
        }

        public static uint RunIdle (IdleHandler handler)
        {
            if (idle_handler == null) {
                throw new NotImplementedException ("The application client must provide an IdleImplementationHandler");
            }

            return idle_handler (handler);
        }

        public static uint RunTimeout (uint milliseconds, TimeoutHandler handler)
        {
            if (timeout_handler == null) {
                throw new NotImplementedException ("The application client must provide a TimeoutImplementationHandler");
            }

            return timeout_handler (milliseconds, handler);
        }

        public static bool IdleTimeoutRemove (uint id)
        {
            if (idle_timeout_remove_handler == null) {
                throw new NotImplementedException ("The application client must provide a IdleTimeoutRemoveImplementationHandler");
            }

            return idle_timeout_remove_handler (id);
        }

        private static void Dispose ()
        {
            ServiceManager.JobScheduler.CancelAll (true);
            ServiceManager.Shutdown ();

            lock (running_clients) {
                while (running_clients.Count > 0) {
                    running_clients.Pop ().Dispose ();
                }
            }
        }

        private static ShutdownRequestHandler shutdown_prompt_handler = null;
        public static ShutdownRequestHandler ShutdownPromptHandler {
            get { return shutdown_prompt_handler; }
            set { shutdown_prompt_handler = value; }
        }

        private static TimeoutImplementationHandler timeout_handler = null;
        public static TimeoutImplementationHandler TimeoutHandler {
            get { return timeout_handler; }
            set { timeout_handler = value; }
        }

        private static IdleImplementationHandler idle_handler = null;
        public static IdleImplementationHandler IdleHandler {
            get { return idle_handler; }
            set { idle_handler = value; }
        }

        private static IdleTimeoutRemoveImplementationHandler idle_timeout_remove_handler = null;
        public static IdleTimeoutRemoveImplementationHandler IdleTimeoutRemoveHandler {
            get { return idle_timeout_remove_handler; }
            set { idle_timeout_remove_handler = value; }
        }

        public static string InternalName {
            get { return "banshee"; }
        }

        public static string IconName {
            get { return "media-player-banshee"; }
        }

        private static string api_version;
        public static string ApiVersion {
            get {
                if (api_version != null) {
                    return api_version;
                }

                try {
                    AssemblyName name = Assembly.GetEntryAssembly ().GetName ();
                    api_version = String.Format ("{0}.{1}.{2}", name.Version.Major,
                        name.Version.Minor, name.Version.Build);
                } catch {
                    api_version = "unknown";
                }

                return api_version;
            }
        }

        private static string version;
        public static string Version {
            get { return version ?? (version = GetVersion ("ReleaseVersion")); }
        }

        private static string display_version;
        public static string DisplayVersion {
            get { return display_version ?? (display_version = GetVersion ("DisplayVersion")); }
        }

        private static string build_time;
        public static string BuildTime {
            get { return build_time ?? (build_time = GetBuildInfo ("BuildTime")); }
        }

        private static string build_host_os;
        public static string BuildHostOperatingSystem {
            get { return build_host_os ?? (build_host_os = GetBuildInfo ("HostOperatingSystem")); }
        }

        private static string build_host_cpu;
        public static string BuildHostCpu {
            get { return build_host_cpu ?? (build_host_cpu = GetBuildInfo ("HostCpu")); }
        }

        private static string build_vendor;
        public static string BuildVendor {
            get { return build_vendor ?? (build_vendor = GetBuildInfo ("Vendor")); }
        }

        private static string build_display_info;
        public static string BuildDisplayInfo {
            get {
                if (build_display_info != null) {
                    return build_display_info;
                }

                build_display_info = String.Format ("{0} ({1}, {2}) @ {3}",
                    BuildVendor, BuildHostOperatingSystem, BuildHostCpu, BuildTime);
                return build_display_info;
            }
        }

        private static string GetVersion (string versionName)
        {
            return GetCustomAssemblyMetadata ("ApplicationVersionAttribute", versionName)
                ?? Catalog.GetString ("Unknown");
        }

        private static string GetBuildInfo (string buildField)
        {
            return GetCustomAssemblyMetadata ("ApplicationBuildInformationAttribute", buildField);
        }

        private static string GetCustomAssemblyMetadata (string attrName, string field)
        {
            Assembly assembly = Assembly.GetEntryAssembly ();
            if (assembly == null) {
                return null;
            }

            foreach (Attribute attribute in assembly.GetCustomAttributes (false)) {
                Type type = attribute.GetType ();
                PropertyInfo property = type.GetProperty (field);
                if (type.Name == attrName && property != null &&
                    property.PropertyType == typeof (string)) {
                    return (string)property.GetValue (attribute, null);
                }
            }

            return null;
        }
    }
}
