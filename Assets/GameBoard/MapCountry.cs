using System;
using System.IO;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;

namespace GameBoard
{
    public enum FactionMembershipStatus
    {
        Unaligned,
        Associate,
        Protectorate,
        Ally,
        InitialMember
    }
    public class MapCountry : MapObject
    {
        private void Start()
        {
            _cadreMaterial = Resources.Load<Material>("Shaders/Cadre");
            _cadreGhostMaterial = Resources.Load<Material>("Shaders/CadreGhost");
            CalculatedColor = color;
        }

        private static Material _cadreMaterial;
        private static Material _cadreGhostMaterial;
        
        public Color color;
        public Color CalculatedColor;
        public Color unitMainColor;
        public Color unitSecondaryColor;
        public MapCountry colonialOverlord;
        public MapFaction faction;
        public FactionMembershipStatus diplomaticallyAlignedFactionMembershipStatus;
        // ReSharper disable once MergeConditionalExpression
        public string  IdeologyName => faction is not null ? faction.name : null;
        [NonSerialized] public Material[] CadreMaterialByUnitType;
        [NonSerialized] public Material[] GhostCadreMaterialByUnitType;
        [NonSerialized] public Material[] CadreHighlightedMaterialByUnitType;
        [NonSerialized] public Texture2D Flag;
        [NonSerialized] public Sprite FlagSprite;

        public void SetFaction(int iFaction, FactionMembershipStatus membershipStatus)
        {
            diplomaticallyAlignedFactionMembershipStatus = membershipStatus;
            if (iFaction == -1)
            {
                faction = null;
                transform.SetParent(Map.countriesWrapper.transform);
            }
            else
            {
                try
                {
                    faction = Map.MapFactionsByID[iFaction];
                    transform.SetParent(faction.transform);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Invalid faction id {iFaction}");
                }
            }
            RecalculateFlag();
        }

        public void SetColonialOverlord(int iColonialOverlord)
        {
            try
            {
                if (iColonialOverlord == -1) colonialOverlord = null;
                else colonialOverlord = Map.MapCountriesByID[iColonialOverlord];
                RecalculateFlag();
            }
            catch (Exception e)
            {
                Debug.LogError($"Invalid colonial overlord id {iColonialOverlord}");
            }
        }

        public void RecalculateColor(bool alsoRecalculateBorders)
        {
            if (colonialOverlord is not null)
            {
                CalculatedColor = (colonialOverlord.color * 2f + color) / 3f;
            }
            else if (faction is not null)
            {
                CalculatedColor = (faction.leader.color * 2f + color) / 3f;
            }
            else if (faction is not null)
            {
                CalculatedColor = color;
            }

            if (alsoRecalculateBorders)
            {
                foreach (var mapBorder in Map.MapBordersByID)
                {
                    foreach (var connectedMapTile in mapBorder.connectedMapTiles)
                    {
                        if (connectedMapTile.mapCountry == this)
                        {
                            mapBorder.RecalculateMaterialRuntimeValues();
                            break;
                        }
                    }
                }
            }
        }
        public void RecalculateFlag()
        {
            const string countryFlagFolder = "Icons/Flags";
            const string fallbackFlagPath = countryFlagFolder + "/fallback";
            string countryFolder = countryFlagFolder + $"/{name}";
            bool useCountryFallbackFlag = true;
            if (IdeologyName != null)
            {
                string ideologicalFlagPath = countryFolder + $"/Flag_{IdeologyName}";
                Flag = Resources.Load<Texture2D>(ideologicalFlagPath);
                FlagSprite = Resources.Load<Sprite>(ideologicalFlagPath);
                useCountryFallbackFlag = false;
            }
            if (useCountryFallbackFlag)
            {
                Flag = Resources.Load<Texture2D>(countryFolder + $"/Flag_Default");
                FlagSprite = Resources.Load<Sprite>(countryFolder + $"/Flag_Default");
                if (Flag is null)
                {
                    Flag = Resources.Load<Texture2D>(countryFolder + $"/Flag_Capitalists");
                    FlagSprite = Resources.Load<Sprite>(countryFolder + $"/Flag_Capitalists");
                }
                if (Flag is null)
                {
                    Debug.LogWarning($"Could not find flag for {name}");
                    Flag = Resources.Load<Texture2D>(fallbackFlagPath);
                    if (Flag is null)
                    {
                        Debug.LogWarning("Could not load fallback flag");
                    }
                }
            }
            
            if (Flag is not null && FlagSprite is null) Debug.LogWarning("Could not load flag for {name} as a sprite. Make sure all the flags are in a supported format (etc .png)");
            
            RecalculateCadreAppearance();
        }
        public void RecalculateCadreAppearance()
        {
            CadreMaterialByUnitType = new Material[Map.Ruleset.unitTypes.Length + 1];
            GhostCadreMaterialByUnitType = new Material[Map.Ruleset.unitTypes.Length + 1];
            for (int i = 0; i < Map.Ruleset.unitTypes.Length; i++)
            {
                UnitType unitType = Map.Ruleset.unitTypes[i];
                (CadreMaterialByUnitType[i], GhostCadreMaterialByUnitType[i]) = GenerateMaterialsForUnitType(unitType);
            }

            (CadreMaterialByUnitType[^1], GhostCadreMaterialByUnitType[^1]) = GenerateMaterialsForUnitType(UnitType.Unknown);
        }
        
        (Material solidMaterial, Material ghostMaterial) GenerateMaterialsForUnitType(UnitType unitType)
        {
            Material material = new Material(_cadreMaterial);
            Material ghostMaterial = new Material(_cadreGhostMaterial);
            material.SetColor("_MainColor", unitMainColor);
            ghostMaterial.SetColor("_MainColor", unitMainColor);
            material.SetColor("_SecondaryColor", unitSecondaryColor);
            ghostMaterial.SetColor("_SecondaryColor", unitSecondaryColor);
            material.SetTexture("_Flag", Flag);
            ghostMaterial.SetTexture("_Flag", Flag);
            material.SetTexture("_MainTex", unitType.Icon);
            ghostMaterial.SetTexture("_MainTex", unitType.Icon);
            return (material, ghostMaterial);
        }

        public Material GetMaterialForCadreUnitType(UnitType unitType, bool useGhostMaterial)
        {
            try
            {
                if (useGhostMaterial)
                {
                    if (unitType == UnitType.Unknown) return GhostCadreMaterialByUnitType[^1];
                    else return GhostCadreMaterialByUnitType[unitType.IdAndInitiative];
                }
                else
                {
                    if (unitType == UnitType.Unknown) return CadreMaterialByUnitType[^1];
                    else return CadreMaterialByUnitType[unitType.IdAndInitiative];
                }

            }
            catch (IndexOutOfRangeException e)
            {
                throw new InvalidOperationException($"Unit type ID ({unitType.IdAndInitiative}) above the length of the unit type materials array ({CadreMaterialByUnitType.Length}) in {name}");
            }

        }
    }
}
