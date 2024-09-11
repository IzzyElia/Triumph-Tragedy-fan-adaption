using System;
using GameBoard;
using GameLogic;
using GameSharedInterfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game_Logic.TriumphAndTragedy.Lobby
{
    [ExecuteAlways]
    public class LobbyFaction : MonoBehaviour
    {
        public Image flagImage;
        public Outline outline;
        public TextMeshProUGUI textMesh;
        public Button button;
        public Color OutlineColorWhenAssigned;
        public Color OutlineColorWhenUnassigned;
        [NonSerialized] public GameFaction Faction;
        [NonSerialized] public Controller Controller;
        private CountryFlagGraphics _flagGraphics = new ();

        private static GameObject _prefab;
        public static LobbyFaction Create(Controller controller, GameFaction faction, UnityClient localClient)
        {
            if (_prefab is null)
            {
                string path = "Prefabs/Lobby/LobbyFaction";
                _prefab = Resources.Load<GameObject>(path);
                if (_prefab is null) throw new MissingResourceException(path);
            }

            LobbyFaction lobbyFaction = Instantiate(_prefab).GetComponent<LobbyFaction>();
            if (lobbyFaction is null) throw new InvalidOperationException();
            
            lobbyFaction.transform.SetParent(controller.factionsListParent.transform);
            lobbyFaction.Controller = controller;
            lobbyFaction.Refresh(faction, localClient);
            return lobbyFaction;
        }

        private string _flagPreviouslyRecalculatedForName = null;
        private string _flagPreviouslyRecalculatedForIdeology = null;
        public void Refresh(GameFaction faction, UnityClient localClient)
        {
            Faction = faction;
            
            if (faction.LeaderCountry.InternalName != _flagPreviouslyRecalculatedForName ||
                faction.IdeologyName != _flagPreviouslyRecalculatedForIdeology)
            {
                _flagPreviouslyRecalculatedForName = faction.LeaderCountry.InternalName;
                _flagPreviouslyRecalculatedForIdeology = faction.IdeologyName;
                _flagGraphics.RecalculateFlag(faction.LeaderCountry.InternalName, faction.IdeologyName);
                flagImage.sprite = _flagGraphics.FlagSprite;
            }
            
            if (localClient is null) throw new ArgumentException();
            try
            {
                ClientsidePlayerData playerMetadata = localClient.PlayerSlots[Faction.ID];
                textMesh.text = playerMetadata.PlayerName == null ? "unnamed player" : playerMetadata.PlayerName;
            }
            catch (Exception e)
            {
                Debug.LogError("Error reading player metadata\n" + e.Message);
            }
            outline.effectColor = localClient.PlayerSlot == Faction.ID
                ? OutlineColorWhenAssigned
                : OutlineColorWhenUnassigned;
        }

        public void OnClick()
        {
            if (Controller.LocalClients.Count > 0)
            {
                Controller.LocalClients[0].AttemptChangePlayerSlot(Faction.ID);
            }
        }

        private void OnValidate()
        {
            outline.effectColor = OutlineColorWhenUnassigned;
        }
    }
}
