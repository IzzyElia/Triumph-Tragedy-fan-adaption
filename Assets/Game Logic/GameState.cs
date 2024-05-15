using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard;
using Unity.Collections;
using UnityEngine;
using System.Reflection;

namespace GameLogic
{
    public class GameState
    {
        static GameState()
        {
            AssignTypeIDs();
        }
        private const byte GameEntityUpdateHeader = 2;
        private const byte AmendEntitiesMapHeader = 1;
        private const byte InitResyncHeader = 0;
        private const byte EndResyncHeader = 4;
        
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
        public Map MapRenderer;

        public GameState(UnityNetworkMember networkMember)
        {
            NetworkMember = networkMember;
            RegisterEntityType<GameTile>(256);
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

        public void SendSync(int targetPlayer)
        {
            Debug.Log($"Sending full sync to player #{targetPlayer}");
            UnityServer server = NetworkMember as UnityServer;
            if (server == null)
            {
                Debug.Log("Is a clientside GameState");
                return;
            }
            DataStreamWriter message = server.CreateNewGameStateMessage(targetPlayer);
            message.WriteByte(InitResyncHeader);
            foreach ((Type type, GameEntity[] entities) in _entitiesMap)
            {
                message.WriteByte(AmendEntitiesMapHeader);
                
                message.WriteByte(GetTypeID(type));
                message.WriteInt(entities.Length);
            }

            message.WriteByte(EndResyncHeader);
            message.WriteInt(EntitiesMapHash);
            server.PushMessage(message);

            foreach ((Type type, GameEntity[] entities) in _entitiesMap)
            {
                foreach (GameEntity entity in entities)
                {
                    entity.PushFullState(targetPlayer);
                }
            }
            
        }

        public DataStreamWriter CreateEntityUpdateMessage(int targetPlayer, Type type, int id)
        {
            UnityServer server = NetworkMember as UnityServer;
            if (server == null)
            {
                Debug.Log("Is a clientside GameState");
                return default;
            }

            DataStreamWriter outgoingMessage = server.CreateNewGameStateMessage(targetPlayer);
            outgoingMessage.WriteByte(GameEntityUpdateHeader);
            outgoingMessage.WriteByte(GetTypeID(type));
            outgoingMessage.WriteInt(id);
            return outgoingMessage;
        }

        public void ReceiveAndRouteMessage(DataStreamReader message)
        {
            byte header = message.ReadByte();

            switch (header)
            {
                case InitResyncHeader:
                    // Recieve resync
                    _entitiesMap.Clear();
                    while (message.ReadByte() == AmendEntitiesMapHeader)
                    {
                        ReadAndApplyType(message);
                    }

                    int expectedEntitiesMapHash = message.ReadInt();
                    Debug.Log($"{expectedEntitiesMapHash} <-> {EntitiesMapHash}");
                    if (EntitiesMapHash != expectedEntitiesMapHash)
                    {
                        Debug.LogWarning("Sync failed. Disconnecting");
                        NetworkMember.Dispose();
                    }
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
                    entity.ReceiveUpdate(message);
                    break;
            }
        }

        /// <summary>
        /// Gets the entity of the specified type with the specified ID, creating a new one if
        /// it doesn't exist.
        /// </summary>
        public GameEntity GetOrCreateEntity(Type entityType, int id)
        {
            if (_entitiesMap.TryGetValue(entityType, out GameEntity[] entities))
            {
                if (id >= 0 && id < entities.Length)
                {
                    return entities[id];
                }
            }
            else
            {
                GameEntity entity = GameEntity.Create(entityType, this);
                _entitiesMap[entityType][id] = entity;
            }
            return null;
        }

        void ReadAndApplyType(DataStreamReader message)
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
            Debug.Log($"Applying type {type.Name}");
        }
        
        
        // Type mapping -----------------------
        // Map each type to a byte id for network synchronization
        protected static Dictionary<byte, Type> typeMap = new Dictionary<byte, Type>();
        protected static Dictionary<Type, byte> reverseTypeMap = new Dictionary<Type, byte>();
        public static int TypesHash;

        static void AssignTypeIDs()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(GameEntity));

            var gameEntityTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(GameEntity)))
                .OrderBy(t => t.FullName) // Order types by their full name to ensure consistent ordering.
                .ToList();

            if (gameEntityTypes.Count > byte.MaxValue)
            {
                throw new InvalidOperationException($"Cannot map more than {byte.MaxValue} types. Found {gameEntityTypes.Count} subtypes of GameEntity.");
            }

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
                throw new KeyNotFoundException();
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
                Debug.LogError($"Type not mapped {type.FullName}");
                return 0;
            }
        }
    }
}