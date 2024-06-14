using System;
using System.IO;
using GameSharedInterfaces;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace GameBoard
{
    public class MapCountry : MapObject
    {
        private void Start()
        {
            _cadreMaterial = Resources.Load<Material>("Shaders/Cadre");
        }

        private static Material _cadreMaterial;
        
        public Color color;
        public Color unitMainColor;
        public Color unitSecondaryColor;
        public MapCountry colonialOverlord;
        public MapFaction faction;
        // ReSharper disable once MergeConditionalExpression
        public string  IdeologyName => faction is not null ? faction.name : null;
        [NonSerialized] public Material[] CadreMaterialByUnitType;
        [NonSerialized] public Material[] CadreHighlightedMaterialByUnitType;
        public Texture2D Flag;

        public void SetFaction(int iFaction)
        {
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
                useCountryFallbackFlag = false;
            }
            if (useCountryFallbackFlag)
            {
                Flag = Resources.Load<Texture2D>(countryFolder + $"/Flag_Default");
                if (Flag is null)
                {
                    Flag = Resources.Load<Texture2D>(countryFolder + $"/Flag_Capitalists");
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
            
            RecalculateCadreAppearance();
        }
        public void RecalculateCadreAppearance()
        {
            CadreMaterialByUnitType = new Material[Map.Ruleset.UnitTypes.Length + 1];
            for (int i = 0; i < Map.Ruleset.UnitTypes.Length; i++)
            {
                UnitType unitType = Map.Ruleset.UnitTypes[i];
                CadreMaterialByUnitType[i] = GenerateMaterialForUnitType(unitType);
            }

            CadreMaterialByUnitType[^1] = GenerateMaterialForUnitType(UnitType.Unknown);
        }
        
        Material GenerateMaterialForUnitType(UnitType unitType)
        {
            Material material = new Material(_cadreMaterial);
            material.SetColor("_MainColor", unitMainColor);
            material.SetColor("_SecondaryColor", unitSecondaryColor);
            material.SetTexture("_Flag", Flag);
            material.SetTexture("_MainTex", unitType.Icon);
            return material;
        }

        public Material GetMaterialForCadreUnitType(UnitType unitType)
        {
            try
            {
                if (unitType == UnitType.Unknown) return CadreMaterialByUnitType[^1];
                else return CadreMaterialByUnitType[unitType.IdAndInitiative];
            }
            catch (IndexOutOfRangeException e)
            {
                throw new InvalidOperationException($"Unit type ID ({unitType.IdAndInitiative}) above the length of the unit type materials array ({CadreMaterialByUnitType.Length}) in {name}");
            }

        }
    }
}
