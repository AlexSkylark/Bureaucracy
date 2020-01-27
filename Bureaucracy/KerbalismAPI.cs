using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KSP.UI.Screens.Settings;
using UnityEngine;

namespace Bureaucracy
{
    class KerbalismApi
    {
        private static bool available = false;
        private static Type kerbalismApi = null;
        private FieldInfo addScienceBlocker;
        private FieldInfo enableEvent;

        private bool Available()
        {
            foreach (var a in AssemblyLoader.loadedAssemblies)
            {
                if (kerbalismApi != null && addScienceBlocker != null && enableEvent != null) return true;
                // name will be "Kerbalism" for debug builds,
                // and "Kerbalism18" or "Kerbalism16_17" for releases
                // there also is a KerbalismBootLoader, possibly a KerbalismContracts and other mods
                // that start with Kerbalism, so explicitly request equality or test for anything
                // that starts with Kerbalism1
                Debug.Log("[Bureaucracy]: Attempting to find Kerbalism");
                if (a.name.Equals("Kerbalism") || a.name.StartsWith("Kerbalism1", StringComparison.Ordinal))
                {
                    kerbalismApi = a.assembly.GetType("KERBALISM.API");
                    Debug.Log("Found KERBALISM API in " + a.name + ": " + kerbalismApi);
                    if (kerbalismApi != null)
                    {
                        addScienceBlocker = kerbalismApi.GetField("preventScienceCrediting", BindingFlags.Public | BindingFlags.Static);
                        enableEvent = kerbalismApi.GetField("subjectsReceivedEventEnabled", BindingFlags.Public | BindingFlags.Static);
                    }
                    available = kerbalismApi != null;
                    Debug.Log("[Bureaucracy]: Kerbalism found: " + available);
                }
            }
            return available;
        }

        public bool ActivateKerbalismInterface()
        {
            if (!Available()) return false;
            if (addScienceBlocker == null || enableEvent == null) return false;
            addScienceBlocker.SetValue(null, true);
            enableEvent.SetValue(null, true);
            return (bool) enableEvent.GetValue(kerbalismApi);
        }
    }
}