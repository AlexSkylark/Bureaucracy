using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Upgradeables;

namespace Bureaucracy
{
    public class BureaucracyFacility
    {
        private int level;
        private readonly int upkeepCost;
        private bool recentlyUpgraded;
        public FacilityUpgradeEvent Upgrade;
        private bool isClosed;
        public bool IsPriority;
        public int LaunchesThisMonth;

        public int Level => level;

        public bool IsClosed => isClosed;

        private bool CanBeClosed { get; set; }

        public void ReopenFacility()
        {
            if (!isClosed) return;
            Debug.Log("[Bureaucracy]: Reopening " + Name);
            LaunchesThisMonth = 0;
            isClosed = false;
        }

        public void CloseFacility()
        {
            if (!CanBeClosed) return;
            isClosed = true;
            Debug.Log("[Bureaucracy]: " + Name + " has been closed");
        }

        public bool Upgrading { get; private set; }

        public int MaintenanceCost => upkeepCost * level;

        public string Name { get; }

        public BureaucracyFacility(SpaceCenterFacility spf)
        {
            Name = SetName(spf);
            upkeepCost = SetCosts();
            level = GetFacilityLevel();
            Debug.Log("[Bureaucracy]: Setup Facility " + Name);
        }

        private int GetFacilityLevel()
        {
            foreach (KeyValuePair<string, ScenarioUpgradeableFacilities.ProtoUpgradeable> config in ScenarioUpgradeableFacilities.protoUpgradeables)
            {
                if (!config.Key.Contains(Name)) continue;
                List<UpgradeableFacility> upgradeables = config.Value.facilityRefs;
                foreach (UpgradeableFacility upgradeableBuilding in upgradeables)
                {
                    return upgradeableBuilding.FacilityLevel;
                }
            }
            return level;
        }

        private string SetName(SpaceCenterFacility spf)
        {
            switch (spf)
            {
                case SpaceCenterFacility.Administration:
                    return "Administration";
                case SpaceCenterFacility.AstronautComplex:
                    CanBeClosed = true;
                    return "AstronautComplex";
                case SpaceCenterFacility.MissionControl:
                    return "MissionControl";
                case SpaceCenterFacility.SpaceplaneHangar:
                    CanBeClosed = true;
                    return "SpaceplaneHangar";
                case SpaceCenterFacility.TrackingStation:
                    return "TrackingStation";
                case SpaceCenterFacility.ResearchAndDevelopment:
                    return "ResearchAndDevelopment";
                case SpaceCenterFacility.VehicleAssemblyBuilding:
                    CanBeClosed = true;
                    return "VehicleAssemblyBuilding";
                case SpaceCenterFacility.Runway:
                    return "Runway";
                case SpaceCenterFacility.LaunchPad:
                    return "LaunchPad";
                default:
                    return "Other Facility";
            }
        }

        private int SetCosts()
        {
            int cost;
            switch (Name)
            {
                case "Administration":
                    cost = SettingsClass.Instance.AdminCost;
                    break;
                case "AstronautComplex":
                    cost = SettingsClass.Instance.AstronautComplexCost;
                    break;
                case "MissionControl":
                    cost = SettingsClass.Instance.MissionControlCost;
                    break;
                case "SpaceplaneHangar":
                    cost = SettingsClass.Instance.SphCost;
                    break;
                case "TrackingStation":
                    cost = SettingsClass.Instance.TrackingStationCost;
                    break;
                case "ResearchAndDevelopment":
                    cost = SettingsClass.Instance.RndCost;
                    break;
                case "VehicleAssemblyBuilding":
                    cost = SettingsClass.Instance.VabCost;
                    break;
                case "Other Facility":
                    cost = SettingsClass.Instance.OtherFacilityCost;
                    break;
                default:
                    cost = 0;
                    break;
            }

            return cost;
        }

        public void StartUpgrade(UpgradeableFacility facilityToUpgrade)
        {
            Upgrade = new FacilityUpgradeEvent(facilityToUpgrade.id, this);
            Upgrading = true;
            ScreenMessages.PostScreenMessage("[Bureaucracy]: Upgrade of " + Name + " requested");
            Debug.Log("[Bureaucracy]: Upgrade of " + Name + " requested for " + Upgrade.OriginalCost);
        }

        public string GetProgressReport(FacilityUpgradeEvent upgrade)
        {
            // ReSharper disable once BuiltInTypeReferenceStyleForMemberAccess
            if (!Upgrading && !recentlyUpgraded) return String.Empty;
            if (!recentlyUpgraded) return Name + ": $" + (upgrade.OriginalCost-upgrade.RemainingInvestment) + " / " + upgrade.OriginalCost;
            recentlyUpgraded = false;
            return Name + ": Upgrade completed successfully";
        }

        public void OnLoad(ConfigNode[] facilityNodes)
        {
            for (int i = 0; i < facilityNodes.Length; i++)
            {
                ConfigNode cn = facilityNodes.ElementAt(i);
                if (cn.GetValue("Name") != Name) continue;
                int.TryParse(cn.GetValue("Level"), out level);
                bool.TryParse(cn.GetValue("RecentlyUpgraded"), out recentlyUpgraded);
                bool.TryParse(cn.GetValue("Closed"), out isClosed);
                bool.TryParse(cn.GetValue("isPriority"), out IsPriority);
                int.TryParse(cn.GetValue("LaunchesThisMonth"), out LaunchesThisMonth);
                SetCosts();
                ConfigNode upgradeNode = cn.GetNode("UPGRADE");
                if (upgradeNode == null) return;
                Upgrading = true;
                Upgrade = new FacilityUpgradeEvent(upgradeNode.GetValue("ID"), this);
                Upgrade.OnLoad(upgradeNode);
            }
        }

        public void OnSave(ConfigNode cn)
        {
            ConfigNode thisNode = new ConfigNode("FACILITY");
            thisNode.SetValue("Name", Name, true);
            thisNode.SetValue("isPriority", IsPriority, true);
            thisNode.SetValue("Level", level, true);
            thisNode.SetValue("RecentlyUpgraded", recentlyUpgraded, true);
            thisNode.SetValue("Closed", isClosed, true);
            thisNode.SetValue("LaunchesThisMonth", LaunchesThisMonth, true);
            if (Upgrading) Upgrade.OnSave(thisNode);
            cn.AddNode(thisNode);
        }

        public void OnUpgradeCompleted()
        {
            Upgrading = false;
            Upgrade = null;
            recentlyUpgraded = true;
            IsPriority = false;
            level++;
            Debug.Log("[Bureaucracy]: Upgrade of " + Name + " completed");
        }

        public bool IsDestroyed()
        {
            foreach (DestructibleBuilding building in Destructibles())
            {
                if (building.IsDestroyed) return true;
            }

            return false;
        }

        public void DestroyBuilding()
        {
            if (!CanBeDestroyed()) return;
            foreach (DestructibleBuilding building in Destructibles())
            {
                building.Demolish();
            }
        }

        public bool CanBeDestroyed()
        {
            return level > 2;
        }

        private List<DestructibleBuilding> Destructibles()
        {
            foreach (KeyValuePair<string, ScenarioDestructibles.ProtoDestructible> kvp in ScenarioDestructibles.protoDestructibles)
            {
                if (!kvp.Key.Contains(Name)) continue;
                return kvp.Value.dBuildingRefs;
            }
            return null;
        }
    
        public void CancelUpgrade()
        {
            Upgrade = null;
            Upgrading = false;
            IsPriority = false;
            Debug.Log("[Bureaucracy]: Upgrade of "+Name+" cancelled");
            ScreenMessages.PostScreenMessage("[Bureaucracy]: " + Name + " - Upgrade cancelled");
        }
    }
}