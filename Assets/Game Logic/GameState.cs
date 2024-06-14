using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameBoard;
using Unity.Collections;
using UnityEngine;
using System.Reflection;
using Game_Logic;
using GameBoard.UI;
using GameSharedInterfaces;

namespace GameLogic
{
    public abstract class GameState : IGameState
    {
        static GameState()
        {
            AssignTypeIDs();
        }
        
        public const byte AmendEntitiesMapHeader = 99;
        public const byte InitResyncHeader = 0;
        public const byte GameEntityUpdateHeader = 2;
        public const byte GameStateUpdateHeader = 3;
        public const byte EndResyncHeader = 4;
        
        private Dictionary<Type, GameEntity[]> _entitiesMap = new();
        public int EntitiesMapHash
        {
            get
            {
                int hashCode = 17;
                foreach (var pair in _entitiesMap)
                {
                    hashCode ^= pair.Key.GetHashCode();
                    hashCode ^= pair.Value.Length.GetHashCode();
                }
                return hashCode;
            }
        }
        public UnityNetworkMember NetworkMember;
        public bool HasStartedInitialSync { get; private set; } = false;
        public IReadOnlyCollection<int> Players
        {
            get
            {
                UnityServer server = NetworkMember as UnityServer;
                if (server == null) throw new InvalidOperationException("Iterating over players supported serverside (right now)");
                return server.FilledPlayerSlots;
            }
        }


        private int _playerCount = -1;

        public int PlayerCount
        {
            get => _playerCount;
            protected set
            {
                _playerCount = value;
            }
        }

        public string MapName;
        public string RulesetName;
        public bool IsServer => NetworkMember is UnityServer;
        public Map MapRenderer;
        public IRuleset Ruleset { get; set; }
        public UIController UIController;
        /// <summary>
        /// Flags that the gamestate is a clientside gamestate being resynced
        /// </summary>
        public bool IsBeingResynced { get; private set; } = false;

        public bool IsSynced { get; private set; } = false;
        public bool IsWaitingOnNetworkReply => NetworkMember.WaitingOnReply;

        public int iPlayer => IsServer ? -1 : ((UnityClient)NetworkMember).PlayerSlot; // The player the gamestate is for
        public abstract int ActivePlayer { get; }
        public abstract bool IsWaitingOnPlayer(int iPlayer);


        public GameState()
        {
        }

        /// <summary>
        /// Fully refresh the map renderer from scratch
        /// </summary>
        protected abstract void FullyRefreshMapRenderer();
        public abstract void CalculateDerivedTileAndBorderValues(Map map);
        public abstract void ReceiveGameStateUpdate(ref DataStreamReader incomingMessage);
        protected DataStreamWriter StartGameStateUpdate(int targetPlayer)
        {
            try
            {
                UnityServer server = (UnityServer)NetworkMember;
                server.CreateNewGameStateMessage(targetPlayer, out DataStreamWriter message);
                message.WriteByte(GameStateUpdateHeader);
                return message;
            }
            catch (InvalidCastException e)
            {
                throw new InvalidOperationException("GameState is clientside");
            }
        }
        protected void PushGameStateUpdate(ref DataStreamWriter outgoingMessage, int targetPlayer)
        {
            try
            {
                UnityServer server = (UnityServer)NetworkMember;
                server.PushMessage(ref outgoingMessage, targetPlayer);
            }
            catch (InvalidCastException e)
            {
                throw new InvalidOperationException("GameState is clientside");
            }
        }

        public DataStreamWriter CreateEntityUpdateMessage(int targetPlayer, Type type, int id)
        {
            UnityServer server = NetworkMember as UnityServer;
            if (server == null)
            {
                Debug.LogError("Is a clientside GameState");
                return default;
            }
            
            server.CreateNewGameStateMessage(targetPlayer, out DataStreamWriter outgoingMessage);
            outgoingMessage.WriteByte(GameEntityUpdateHeader);
            outgoingMessage.WriteByte(GetTypeID(type));
            outgoingMessage.WriteInt(id);
            return outgoingMessage;
        }


        public void StartGame()
        {
            if (!IsServer) throw new InvalidOperationException("GameState.StartGame() is used to initialize the game serverside. It should not be called by clients");
            Debug.Log($"Starting game with {PlayerCount} players");
            OnGameStart();
        }
        /// <summary>
        /// Called on the server side when starting the game
        /// </summary>
        public abstract void OnGameStart();
        /// <summary>
        /// Called on the client side after the entities map has been recreated but before any of their values have been retrieved
        /// </summary>
        protected abstract void OnSyncStarted();

        public void SetUIActive(bool active)
        {
            UIController.ActiveLocally = active;
        }
        public bool IsPlayerSyncedOrBeingResynced(int iPlayer)
        {
            if (NetworkMember is UnityServer server)
            {
                return server.IsPlayerSyncedOrBeingResynced(iPlayer);
            }

            throw new InvalidOperationException("Can only be called by the server");
        }

        
        // SETUP METHODS
        /// <summary>
        /// Register an entity type that will be used by the game state
        /// </summary>
        /// <param name="bufferSize">The number of instances of the entity to store</param>
        public void RegisterEntityType<T>(int cap) where T : GameEntity
        {
            _entitiesMap.Add(typeof(T), new GameEntity[cap]);
        }
        
        /// <returns>A collection of entities that need to be sync'd</returns>
        public Queue<GameEntity> StartSync(int targetPlayer)
        {
            Debug.Log($"Sending full sync to player #{targetPlayer}");
            if (PlayerCount == -1) Debug.LogError("PlayerCount not set");
            UnityServer server = NetworkMember as UnityServer;
            if (server == null)
            {
                Debug.Log("Is a clientside GameState");
                return null;
            }
            server.CreateNewGameStateMessage(targetPlayer, out DataStreamWriter message);
            message.WriteByte(InitResyncHeader);
            message.WriteInt(PlayerCount);
            message.WriteInt(_entitiesMap.Count);
            foreach ((Type type, GameEntity[] entities) in _entitiesMap)
            {
                message.WriteByte(GetTypeID(type)); 
                message.WriteInt(entities.Length);
            }

            message.WriteInt(EntitiesMapHash);
            server.PushMessage(ref message, targetPlayer);
            
            OnSendingSync(targetPlayer);

            Queue<GameEntity> allEntities = new Queue<GameEntity>();
            foreach ((Type type, GameEntity[] entities) in _entitiesMap)
            {
                foreach (GameEntity entity in entities)
                {
                    if (entity != null)
                        allEntities.Enqueue(entity);
                }
            }

            return allEntities;
        }
        public abstract void OnSendingSync(int targetPlayer);

        public List<ICard> GetCardsInHand(int player)
        {
            List<ICard> cards = new List<ICard>();
            GameCard[] allCards = GameCard.GetAllCards(this);
            for (int i = 0; i < allCards.Length; i++)
            {
                if (allCards[i].HoldingPlayer == player) cards.Add(allCards[i]);
            }

            return cards;
        }

        
        public void ReceiveAndRouteMessage(ref DataStreamReader message)
        {
            byte header = message.ReadByte();

            switch (header)
            {
                case EndResyncHeader:
                    NetworkMember.NetworkingLog($"finishing resync");
                    IsBeingResynced = false;
                    IsSynced = true;
                    CalculateDerivedTileAndBorderValues(MapRenderer);
                    FullyRefreshMapRenderer();
                    UIController.UnresolvedStateChange = true;
                    UIController.UnresolvedResync = true;
                    break;
                case InitResyncHeader:
                    NetworkMember.NetworkingLog($"starting resync");
                    _entitiesMap.Clear();
                    IsBeingResynced = true;
                    IsSynced = false;
                    HasStartedInitialSync = true;
                    PlayerCount = message.ReadInt();
                    int numEntityTypes = message.ReadInt();
                    for (int i = 0; i < numEntityTypes; i++)
                    {
                        ReadAndApplyType(ref message);
                    }

                    int expectedEntitiesMapHash = message.ReadInt();
                    NetworkMember.NetworkingLog($"Hashes match - {expectedEntitiesMapHash} <-> {EntitiesMapHash}", DebuggingLevel.Always);
                    if (EntitiesMapHash != expectedEntitiesMapHash)
                    {
                        Debug.LogWarning("Sync failed. Disconnecting");
                        NetworkMember.Dispose();
                    }
                    OnSyncStarted();
                    // The rest of the resync will come on its own later in the form of individual entity updates
                    break;

                case GameEntityUpdateHeader:
                    byte bType = message.ReadByte();
                    int id = message.ReadInt();
                    Type type;

                    try
                    {
                        type = GetTypeFromID(bType);
                    }
                    catch (KeyNotFoundException)
                    {
                        Debug.LogWarning("No entity type associated with ID");
                        return;
                    }

                    GameEntity entity = GetOrCreateEntity(type, id);
                    entity.ReceiveUpdate(ref message);
                    UIController.UnresolvedStateChange = true;
                    break;
                case GameStateUpdateHeader:
                    // Only accept game state updates if the initial sync has been started. Otherwise, discard them
                    if (HasStartedInitialSync) ReceiveGameStateUpdate(ref message);
                    else Debug.LogWarning("Client received game state data despite not having begun a sync");
                    UIController.UnresolvedStateChange = true;
                    break;
            }
        }

        public T GetEntity<T>(int id) where T : GameEntity => (T)GetEntity(typeof(T), id);
        /// <summary>
        /// Gets the entity of the specified type with the specified ID, returning null if it does not exist
        /// </summary>
        public GameEntity GetEntity(Type entityType, int id)
        {
            if (_entitiesMap.TryGetValue(entityType, out GameEntity[] entities))
            {
                if (id >= 0 && id < entities.Length)
                {
                    return entities[id];
                }
            }

            return null;
        }
        public T GetOrCreateEntity<T>(int id) where T : GameEntity => (T)GetOrCreateEntity(typeof(T), id);
        /// <summary>
        /// Gets the entity of the specified type with the specified ID, creating a new one if
        /// it doesn't exist.
        /// </summary>
        public GameEntity GetOrCreateEntity(Type entityType, int id)
        {
            GameEntity[] entities = _entitiesMap[entityType];
            if (id >= 0 && id < entities.Length)
            {
                if (entities[id] == null)
                {
                    GameEntity entity = GameEntity.Create(entityType, this);
                    _entitiesMap[entityType][id] = entity;
                    entity.ID = id;
                    return entity;
                }
                else
                {
                    return entities[id];
                }
            }
            else
            {
                throw new ArgumentException("ID not within the valid range for the entity type");
            }
        }
        void ReadAndApplyType(ref DataStreamReader message)
        {
            byte typeId = message.ReadByte();
            int size = message.ReadInt();
            Type type = GetTypeFromID(typeId);
            _entitiesMap.Remove(type);
            _entitiesMap.Add(type, new GameEntity[size]);
            for (int i = 0; i < size; i++)
            {
                GameEntity gameEntity = GameEntity.Create(type, this);
                gameEntity.ID = i;
                gameEntity.GameState = this;
                _entitiesMap[type][i] = gameEntity;
            }
        }

        public GameEntity[] GetEntitiesOfType(Type type)
        {
            return _entitiesMap[type];
        }
        public T[] GetEntitiesOfType<T>() where T : GameEntity
        {
            GameEntity[] entities = _entitiesMap[typeof(T)];
            T[] t = new T[entities.Length];
            for (int i = 0; i < t.Length; i++)
            {
                t[i] = (T)entities[i];
            }
            return t;
        }

        public List<GameEntity> GetAllEntities()
        {
            List<GameEntity> allEntities = new List<GameEntity>();
            foreach ((Type type, GameEntity[] entities) in _entitiesMap)
            {
                foreach (GameEntity entity in entities)
                {
                    if (entity != null) allEntities.Add(entity);
                }
            }

            return allEntities;
        }

        public int MaxEntityID(Type type)
        {
            return _entitiesMap[type].Length;
        }

        public int MaxEntityID<T>() => MaxEntityID(typeof(T));

        private const bool LogHashInfo = false;
        public int GetStateHash(int asPlayer, string debuggingFile = "HashLog.txt")
        {
            int hash = 17;
            StreamWriter fileStream;
            if (LogHashInfo) fileStream = new StreamWriter(Application.dataPath + $"/{debuggingFile}");
            unchecked
            {
                
                GameEntity[] entities;
                for (int i = 0; i < _numTypes; i++)
                {
                    if (_entitiesMap.TryGetValue(GetTypeFromID((byte)i), out entities))
                    {
                        hash ^= i.GetHashCode();
                        for (int j = 0; j < entities.Length; j++)
                        {
                            if (entities[j] != null && entities[j].Active)
                            {
                                hash ^= j.GetHashCode();
                                hash *= entities[j].HashFullState(asPlayer);
                                if (LogHashInfo) fileStream.WriteLine($"{GetTypeFromID((byte)i).Name} #{j}: {entities[j].HashFullState(asPlayer)} (!{hash}!)");
                            }
                        }
                    }
                }
            }
            if (LogHashInfo) fileStream.Close();
            return hash;
        }
        
        
        // Type mapping -----------------------
        // Map each type to a byte id for network synchronization
        protected static Dictionary<byte, Type> typeMap = new Dictionary<byte, Type>();
        protected static Dictionary<Type, byte> reverseTypeMap = new Dictionary<Type, byte>();
        public static int TypesHash;
        private static int _numTypes;
        static void AssignTypeIDs()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(GameEntity));

            var gameEntityTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(GameEntity)) && !t.IsAbstract)
                .OrderBy(t => t.FullName) // Order types by their full name to ensure consistent ordering.
                .ToList();

            if (gameEntityTypes.Count > byte.MaxValue)
            {
                throw new InvalidOperationException($"Cannot map more than {byte.MaxValue} types. Found {gameEntityTypes.Count} subtypes of GameEntity.");
            }

            _numTypes = gameEntityTypes.Count;

            // Assign a byte id to each type.
            byte id = 0;
            foreach (var type in gameEntityTypes)
            {
                typeMap.Add(id, type);
                reverseTypeMap.Add(type, id);
                id++;
            }
            
            // Concatenate the full names of all types.
            var concatenatedTypes = gameEntityTypes.Aggregate("", (current, type) => current + type.FullName);
            TypesHash = concatenatedTypes.GetHashCode();
        }
        public static void SetTypeID(Type type, byte id)
        {
            if (typeMap.ContainsKey(id) || reverseTypeMap.ContainsKey(type))
            {
                typeMap.Remove(id);
                reverseTypeMap.Remove(type);
            }
            typeMap.Add(id, type);
            reverseTypeMap.Add(type, id);
        }
        public static Type GetTypeFromID(byte id)
        {
            try
            {
                return typeMap[id];
            }
            catch (KeyNotFoundException e)
            {
                throw new KeyNotFoundException("Type not found. Has it been registered in the game state");
            } 
        }
        public static byte GetTypeID(Type type)
        {
            try
            {
                return reverseTypeMap[type];
            }
            catch (KeyNotFoundException e)
            {
                Debug.LogError($"Type {type.FullName} not found. Has it been registered in the game state");
                return 0;
            }
        }
        public IPlayerAction GenerateClientsidePlayerActionByName(string name) => PlayerAction.GenerateClientsidePlayerActionByName(this, name);
    }
}