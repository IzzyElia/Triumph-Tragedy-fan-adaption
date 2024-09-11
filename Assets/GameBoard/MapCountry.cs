using System;
using System.Collections.Generic;
using System.IO;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Serialization;

namespace GameBoard
{
    public class CountryFlagGraphics
    {
        [NonSerialized] public Texture2D Flag;
        [NonSerialized] public Sprite FlagSprite;
        [NonSerialized] public Texture2D OverlayFlag;
        [NonSerialized] public Sprite OverlayFlagSprite;
        public void RecalculateFlag(string name, string ideologyName)
        {
            const string countryFlagFolder = "Icons/Flags";
            const string fallbackFlagPath = countryFlagFolder + "/fallback";
            string countryFolder = countryFlagFolder + $"/{name}";
            if (ideologyName != null)
            {
                string ideologicalFlagPath = countryFolder + $"/Flag_{ideologyName}";
                string ideologicalOverlayFlagPath = countryFolder + $"/Flag_{ideologyName}_Overlay";
                Flag = Resources.Load<Texture2D>(ideologicalFlagPath);
                FlagSprite = Resources.Load<Sprite>(ideologicalFlagPath);
                OverlayFlag = Resources.Load<Texture2D>(ideologicalOverlayFlagPath);
                OverlayFlagSprite = Resources.Load<Sprite>(ideologicalOverlayFlagPath);
            }
            if (Flag is null)
            {
                Flag = Resources.Load<Texture2D>(countryFolder + $"/Flag_Default");
                FlagSprite = Resources.Load<Sprite>(countryFolder + $"/Flag_Default");
                OverlayFlag = Resources.Load<Texture2D>(countryFolder + $"/Flag_Default_Overlay");
                OverlayFlagSprite = Resources.Load<Sprite>(countryFolder + $"/Flag_Default_Overlay");
                if (Flag is null)
                {
                    Flag = Resources.Load<Texture2D>(countryFolder + $"/Flag_Capitalist");
                    FlagSprite = Resources.Load<Sprite>(countryFolder + $"/Flag_Capitalist");
                    OverlayFlag = Resources.Load<Texture2D>(countryFolder + $"/Flag_Capitalist_Overlay");
                    OverlayFlagSprite = Resources.Load<Sprite>(countryFolder + $"/Flag_Capitalist_Overlay");
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
            
            if (Flag is not null && FlagSprite is null) Debug.LogWarning($"Could not load flag for {name} as a sprite. Make sure all the flags are in a supported format (etc .png)");
            if (OverlayFlag is not null && OverlayFlagSprite is null) Debug.LogWarning($"Could not load overlay flag for {name} as a sprite. Make sure all the flags are in a supported format (etc .png)");
            if (OverlayFlag is null)
            {
                OverlayFlag = Flag;
                OverlayFlagSprite = FlagSprite;
            }
        }
    }
    public class MapCountry : MapObject
    {
        static void LoadMaterialsIfNeeded()
        {
            if (!_loadedMaterials)
            {
                _cadreMaterial = Resources.Load<Material>("Shaders/Cadre");
                _cadreGhostMaterial = Resources.Load<Material>("Shaders/CadreGhost");
                _loadedMaterials = true;
            }
        }

        private static bool _loadedMaterials;
        private static Material _cadreMaterial;
        private static Material _cadreGhostMaterial;
        
        private void Start()
        {
            CalculatedColor = color;
            if (InternalName == null) InternalName = name;
            DisplayName = InternalName;
        }

        public string InternalName;
        [NonSerialized] public string DisplayName;
        
        public Color color;
        public Color CalculatedColor;
        public Color unitMainColor;
        public Color unitSecondaryColor;
        public MapCountry colonialOverlord;
        public MapFaction associatedFaction;
        public MapTile Capital;

        public MapFaction Faction =>
            associatedFaction is not null && 
            (factionMembershipStatus == FactionMembershipStatus.InitialMember || factionMembershipStatus == FactionMembershipStatus.Ally)
            ? associatedFaction : null;
        public FactionMembershipStatus factionMembershipStatus;
        // ReSharper disable once MergeConditionalExpression
        public string  IdeologyName => Faction is not null ? Faction.IdeologyName : null;
        [NonSerialized] public int AppliedInfluence;
        [NonSerialized] public MapCountry Influencer;
        [NonSerialized] public Material[] CadreMaterialByUnitType;
        [NonSerialized] public Material[] GhostCadreMaterialByUnitType;
        [NonSerialized] public Material[] CadreHighlightedMaterialByUnitType;
        public CountryFlagGraphics FlagGraphics = new ();
        public Texture2D Flag => FlagGraphics.Flag;
        public Sprite FlagSprite => FlagGraphics.FlagSprite;
        public Texture2D OverlayFlag => FlagGraphics.OverlayFlag;
        public Sprite OverlayFlagSprite => FlagGraphics.OverlayFlagSprite;

        public void SetFaction(int iFaction, FactionMembershipStatus membershipStatus, bool recalculateTileAppearance = true)
        {
            factionMembershipStatus = membershipStatus;
            if (iFaction == -1)
            {
                associatedFaction = null;
                transform.SetParent(Map.countriesWrapper.transform);
            }
            else
            {
                try
                {
                    associatedFaction = Map.MapFactionsByID[iFaction];
                    if (Faction is not null) transform.SetParent(associatedFaction.transform);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Invalid faction id {iFaction}");
                }
            }
            RecalculateColor(alsoRecalculateBorders:true, recalculateTileAppearance:true);
            RecalculateFlag();
            foreach (var mapCountry in Map.MapCountriesByID)
            {
                if (mapCountry.colonialOverlord == this) mapCountry.RecalculateColor(alsoRecalculateBorders:true, recalculateTileAppearance:true);
            }
        }

        public void RecalculateTileAppearance()
        {
            foreach (var mapTile in Map.MapTilesByID)
            {
                if (mapTile.mapCountry == this)
                {
                    mapTile.RecalculateMaterialDuringRuntime();
                }
            }
        }

        public void RecalculateOverlayPositioning()
        {
            List<MapTile> mapTilesInCountry = new List<MapTile>();
            float left;
            float top;
            float right;
            float bottom;
            left = float.MaxValue;
            top = float.MinValue;
            right = float.MinValue;
            bottom = float.MaxValue;
            foreach (var tile in Map.MapTilesByID)
            {
                if (tile.mapCountry != this) continue;
                mapTilesInCountry.Add(tile);
                foreach (var vertex in tile.Mesh.vertices)
                {
                    Vector2 vertexWorldPosition = vertex + tile.transform.position;
                    left = Mathf.Min(left, vertexWorldPosition.x);
                    top = Mathf.Max(top, vertexWorldPosition.y);
                    right = Mathf.Max(right, vertexWorldPosition.x);
                    bottom = Mathf.Min(bottom, vertexWorldPosition.y);
                }
            }

            Vector2 overlayPosition = new Vector2(
                x: (right - left) / 2f,
                y: (bottom - top) / 2f
            );
            float overlaySize = 1f / Mathf.Min(
                right - left,
                bottom - top
            );
            foreach (var mapTile in mapTilesInCountry)
            {
                mapTile.OverlayPositionAndSize = new Vector3(
                    overlayPosition.x,
                    overlayPosition.y,
                    overlaySize
                );
            }
        }

        public void SetColonialOverlord(int iColonialOverlord)
        {
            try
            {
                if (iColonialOverlord == -1) colonialOverlord = null;
                else colonialOverlord = Map.MapCountriesByID[iColonialOverlord];
                RecalculateFlag();
                RecalculateColor(alsoRecalculateBorders:true, recalculateTileAppearance:true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Invalid colonial overlord id {iColonialOverlord}");
            }
        }

        public void RecalculateColor(bool alsoRecalculateBorders, bool recalculateTileAppearance = true)
        {
            if (!Map.GameState.IsSynced) return;
            if (colonialOverlord is not null)
            {
                if (colonialOverlord.Faction is null) 
                    CalculatedColor = (colonialOverlord.color * 2f + color) / 3f;
                else
                {
                    colonialOverlord.RecalculateColor(alsoRecalculateBorders:false, recalculateTileAppearance:false);
                    CalculatedColor = (colonialOverlord.CalculatedColor * 2f + color) / 3f;
                }
            }
            else if (Faction is not null)
            {
                CalculatedColor = (Faction.leader.color * 2f + color) / 3f;
            }
            else
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
            
            if (recalculateTileAppearance) RecalculateTileAppearance();
        }
        public void RecalculateFlag()
        {
            if (!Map.GameState.IsSynced) return;
            FlagGraphics.RecalculateFlag(InternalName, IdeologyName);
            RecalculateCadreAppearance();
        }
        public void RecalculateCadreAppearance()
        {
            if (!Map.GameState.IsSynced) return;
            CadreMaterialByUnitType = new Material[Map.Ruleset.unitTypes.Length + 1];
            GhostCadreMaterialByUnitType = new Material[Map.Ruleset.unitTypes.Length + 1];
            for (int i = 0; i < Map.Ruleset.unitTypes.Length; i++)
            {
                UnitType unitType = Map.Ruleset.unitTypes[i];
                (CadreMaterialByUnitType[i], GhostCadreMaterialByUnitType[i]) = GenerateMaterialsForUnitType(unitType);
            }

            (CadreMaterialByUnitType[^1], GhostCadreMaterialByUnitType[^1]) = GenerateMaterialsForUnitType(UnitType.Unknown);

            foreach (var mapCadre in Map.MapCadresByID)
            {
                if (mapCadre is not null) mapCadre.RecalculateAppearance();
            }
        }
        
        (Material solidMaterial, Material ghostMaterial) GenerateMaterialsForUnitType(UnitType unitType)
        {
            LoadMaterialsIfNeeded();
            Material material = new Material(_cadreMaterial);
            Material ghostMaterial = new Material(_cadreGhostMaterial);
            material.SetColor("_MainColor", unitMainColor);
            ghostMaterial.SetColor("_MainColor", unitMainColor);
            material.SetColor("_SecondaryColor", unitSecondaryColor);
            ghostMaterial.SetColor("_SecondaryColor", unitSecondaryColor);
            material.SetTexture("_Flag", Flag);
            ghostMaterial.SetTexture("_Flag", Flag);
            material.SetTexture("_MainTex", unitType.GetIcon(this.name));
            ghostMaterial.SetTexture("_MainTex", unitType.GetIcon(this.name));
            return (material, ghostMaterial);
        }

        public Material GetMaterialForCadreUnitType(UnitType unitType, bool useGhostMaterial)
        {
            if (GhostCadreMaterialByUnitType is null) RecalculateCadreAppearance();
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
                throw new InvalidOperationException(
                    $"Unit type ID ({unitType.IdAndInitiative}) above the length of the unit type materials array ({CadreMaterialByUnitType.Length}) in {name}");
            }
            catch (NullReferenceException e)
            {
                throw new InvalidOperationException($"Unit graphics not yet loaded");
            }
        }
    }
}
