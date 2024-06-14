using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Game_Logic.TriumphAndTragedy;
using GameBoard;
using GameSharedInterfaces;
using Unity.Collections;
using UnityEngine;

namespace GameLogic
{
    public abstract class PlayerAction : IPlayerAction
    {
        protected static List<Type> playerActionTypeMap = new ();
        protected static Dictionary<Type, byte> reversePlayerActionTypeMap = new ();
        protected static Dictionary<string, byte> playerActionIDs = new ();

        protected static void RegisterPlayerActionType<T>(string name = null) where T : IPlayerAction
        {
            if (name == null) name = typeof(T).Name;
            reversePlayerActionTypeMap.Add(typeof(T), (byte)playerActionTypeMap.Count);
            playerActionIDs.TryAdd(name, (byte)playerActionTypeMap.Count);
            playerActionTypeMap.Add(typeof(T));
        }

        public static PlayerAction RecreatePlayerActionServerside(GameState gameState, int typeID, int iPlayerFaction, DataStreamReader incomingMessage)
        {
            Type type = playerActionTypeMap[typeID];
            PlayerAction playerAction = (PlayerAction)Activator.CreateInstance(type);
            playerAction.gameState = gameState;
            playerAction.iPlayerFaction = iPlayerFaction;
            playerAction.Recreate(ref incomingMessage);
            return playerAction;
        }

        public static PlayerAction GenerateClientsidePlayerActionByName(GameState gameState, string name)
        {
            if (playerActionIDs.TryGetValue(name, out byte actionID))
            {
                Type type = playerActionTypeMap[actionID];
                PlayerAction playerAction = (PlayerAction)Activator.CreateInstance(type);
                playerAction.gameState = gameState;
                return playerAction;
            }

            Debug.LogError($"{name} is not a defined player action type");
            return null;
        }


        public int iPlayerFaction { get; private set; }
        private GameState gameState;
        public TTGameState GameState => gameState as TTGameState;

        protected PlayerAction()
        {
            TypeID = reversePlayerActionTypeMap[this.GetType()];
        }

        public byte TypeID;
        public abstract void Execute();
        public abstract (bool, string) TestParameter(params object[] parameter);
        public abstract void AddParameter(params object[] parameter);
        public abstract void SetAllParameters(params object[] parameters);
        public abstract object[] GetParameters();
        public abstract void Reset();

        public abstract (bool, string) Validate();
        public abstract void Recreate(ref DataStreamReader incomingMessage);
        public abstract void Write(ref DataStreamWriter outgoingMessage);

        public void Send(Action<bool> callback)
        {
            UnityClient client = gameState.NetworkMember as UnityClient;
            if (client == null) throw new InvalidOperationException("Player Actions can only be sent by the client. Note that this error may also be thrown if the PlayerAction's GameState was not set (ex if it was created by a constructor instead of by calling CreatePlayerActionOfType()");
            client.AttemptAction(this, callback);
        }
    }
}