using System;
using System.Collections.Generic;
using System.Linq;
using GameBoard.UI.SpecializeComponents.CombatPanel;
using GameLogic;
using GameSharedInterfaces;
using GameSharedInterfaces.Triumph_and_Tragedy;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game_Logic.TriumphAndTragedy
{
    public class GameCombat : IGameCombat
    {
        private const byte CombatRollResultsHeader = 0;
        private static int _uIDTicker = 0;
        private uint _rollUIDTicker = 0; // Not synced
        public TTGameState GameState;
        public int CombatUID { get; private set; }
        public int iTile { get; set; }
        public int iAttackerFaction { get; set; }
        public int iDefenderFaction { get; set; }
        public int initiative { get; set; }
        public int iPhasingPlayer { get; set; }
        public bool DecidingDice { get; set; } = false;
        public int numDiceAvailable { get; private set; } = -1;
        public int StageCounter { get; private set; } = -1;
        public CombatDiceDistribution ProvidedCombatDiceDistribution { get; set; }
        public int[] iSupportingCadres { get; set; } = new int[0];
        public List<CombatRoll> CombatRolls { get; set; } = new List<CombatRoll>();

        private Queue<int> initiativeFiringQueue = new Queue<int>(); // Not synced. Used by the server to remember which player's units are firing next

        public static GameCombat CreateCombat(TTGameState gameState, int iTile, int iAttacker, int iDefender)
        {
            if (!gameState.IsServer) throw new ServerOnlyException();
            GameCombat combat = new GameCombat();
            combat.GameState = gameState;
            combat.CombatUID = _uIDTicker;
            unchecked { _uIDTicker++; }
            combat.iTile = iTile;
            combat.iAttackerFaction = iAttacker;
            combat.iDefenderFaction = iDefender;
            
            return combat;
        }
        
        public IGameCadre[] CalculateInvolvedCadreInterfaces()
        {
            List<IGameCadre> involvedCadres = new List<IGameCadre>();
            
            foreach (var gameCadre in GameState.GetEntitiesOfType<GameCadre>())
            {
                if (gameCadre is null) continue;
                if (gameCadre.iTile == iTile) involvedCadres.Add(gameCadre);
            }

            foreach (var iSupportingCadre in iSupportingCadres)
            {
                GameCadre supportingCadre = GameState.GetEntity<GameCadre>(iSupportingCadre);
                involvedCadres.Add(supportingCadre);
            }

            return involvedCadres.ToArray();
        }
        
        GameCadre[] CalculateInvolvedCadres()
        {
            List<GameCadre> involvedCadres = new List<GameCadre>();
            
            foreach (var gameCadre in GameState.GetEntitiesOfType<GameCadre>())
            {
                if (gameCadre is null) continue;
                if (gameCadre.iTile == iTile) involvedCadres.Add(gameCadre);
            }

            foreach (var iSupportingCadre in iSupportingCadres)
            {
                GameCadre supportingCadre = GameState.GetEntity<GameCadre>(iSupportingCadre);
                involvedCadres.Add(supportingCadre);
            }

            return involvedCadres.ToArray();
        }

        public void AdvanceCombat()
        {
            // Scrap this method.
            //
            // After calling ResolveCurrentInitiative, wait for the phasing player to respond with a DiceDistribution
            // In the execution of the pick dice distribution player action, call RollCombatAtCurrentInitiative and dequeue
            // the rolling player
            // If the firing queue is empty, call AdvanceInitiativeAndResolve(), which in turn calls ResolveCurrentInitiative
            
            // In short
            // AdvanceInitiativeAndResolve() -> ResolveCurrentInitiative() -> queues player rolls ->
            // wait for player to pick roll -> RollCombatAtCurrentInitiative() -> check if all players rolled ->
            // AdvanceInitiativeAndResolve()
        }
        
        public void StartCombat()
        {
            if (!GameState.IsServer) throw new ServerOnlyException();
            if (GameState.ActiveCombat is not null) throw new InvalidOperationException("Combat already in progress");
            
            initiative = 0;
            iPhasingPlayer = -1;
            GameState.ActiveCombat = this;
            AdvanceInitiativeAndResolve();
        }

        public void ResolveCurrentInitiative()
        {
            Debug.Log($"Resolving initiative {initiative}");
            if (!GameState.IsServer) throw new ServerOnlyException();
            
            // Global combat variables
            GameFaction attacker = GameState.GetEntity<GameFaction>(iAttackerFaction);
            GameFaction defender = GameState.GetEntity<GameFaction>(iDefenderFaction);
            GameTile tile = GameState.GetEntity<GameTile>(iTile);
            List<CombatRoll> combatRolls = new List<CombatRoll>();
            GameCadre[] units = CalculateInvolvedCadres();
            
            // Variables at initiative level
            // Find units
            UnitType unitType = GameState.Ruleset.unitTypes[initiative];
            GameCadre[] attackerUnitsAtInitiative = units.Where(unit => unit.Faction == attacker && unit.UnitType == unitType).ToArray();
            GameCadre[] defenderUnitsAtInitiative = units.Where(unit => unit.Faction == defender && unit.UnitType == unitType).ToArray();
            UnitType attackerUnitType = UnitType.GetModifiedUnitType(unitType, attacker.iTechs);
            UnitType defenderUnitType = UnitType.GetModifiedUnitType(unitType, attacker.iTechs);
            bool attackerFiresFirst = attackerUnitType.HasFirstFire && !defenderUnitType.HasFirstFire;
            
            if (attackerFiresFirst)
            {
                Debug.Log("Attacker firing first");
                if (attackerUnitsAtInitiative.Length > 0) initiativeFiringQueue.Enqueue(iAttackerFaction);
                if (defenderUnitsAtInitiative.Length > 0) initiativeFiringQueue.Enqueue(iDefenderFaction);
            }
            else
            {
                Debug.Log("Defender firing first");
                if (defenderUnitsAtInitiative.Length > 0) initiativeFiringQueue.Enqueue(iDefenderFaction);
                if (attackerUnitsAtInitiative.Length > 0) initiativeFiringQueue.Enqueue(iAttackerFaction);
            }

            PassToPlayerControl(units, iPhasingPlayer:initiativeFiringQueue.Peek());
        }
        
        void AdvanceInitiativeAndResolve()
        {
            Debug.Log("Advancing combat initiative...");
            GameCadre[] units = CalculateInvolvedCadres();
            bool thereIsUnitAtInitiative = false;
            while (initiative < GameState.Ruleset.unitTypes.Length)
            {
                thereIsUnitAtInitiative = false;
                for (int i = 0; i < units.Length; i++)
                {
                    if (units[i].UnitType.IdAndInitiative == initiative)
                    {
                        thereIsUnitAtInitiative = true;
                        break;
                    }
                }

                if (thereIsUnitAtInitiative) break;
                initiative++;
            }

            if (thereIsUnitAtInitiative)
            {
                Debug.Log($"Units found at initiative {initiative}");
                ResolveCurrentInitiative();
            }
            else
            {
                EndCombat();
            }
        }

        void RollCombatAtCurrentInitiative(int iPlayer, CombatDiceDistribution combatDiceDistribution)
        {
            string whosrolling = iPlayer == iAttackerFaction ? "Attacker" : "Defender";
            Debug.Log($"{whosrolling} rolling...");
            Random.InitState(Time.time.GetHashCode());

            GameFaction playerFaction = GameState.GetEntity<GameFaction>(iPlayer);
            GameFaction opposingFaction = iPlayer == iAttackerFaction
                ? GameState.GetEntity<GameFaction>(iDefenderFaction)
                : GameState.GetEntity<GameFaction>(iAttackerFaction);
            GameCadre[] units = CalculateInvolvedCadres();
            List<GameCadre> unitsAtInitiativeOfCurrentPlayer = new List<GameCadre>();
            List<GameCadre> opposingGroundUnits = new List<GameCadre>();
            List<GameCadre> opposingAirUnits = new List<GameCadre>();
            List<GameCadre> opposingSeaUnits = new List<GameCadre>();
            List<GameCadre> opposingSubUnits = new List<GameCadre>();
            UnitType firingUnitType = UnitType.GetModifiedUnitType(GameState.Ruleset.unitTypes[initiative], playerFaction.iTechs);
            
            foreach (var cadre in units)
            {
                if (cadre.Faction.ID == iPlayer && cadre.UnitType.IdAndInitiative == initiative) unitsAtInitiativeOfCurrentPlayer.Add(cadre);
                else
                {
                    switch (cadre.UnitType.Category)
                    {
                        case UnitCategory.Ground:
                            opposingGroundUnits.Add(cadre);
                            break;
                        case UnitCategory.Sea:
                            opposingSeaUnits.Add(cadre);
                            break;
                        case UnitCategory.Air:
                            opposingAirUnits.Add(cadre);
                            break;
                        case UnitCategory.Sub:
                            opposingSubUnits.Add(cadre);
                            break;
                        default: throw new NotImplementedException();
                    }
                }
            }

            foreach (var playerCadre in unitsAtInitiativeOfCurrentPlayer)
            {
                CombatRoll combatRoll;
                GameCadre target;
                if (combatDiceDistribution.GroundDice > 0)
                {
                    target = PickTarget(opposingGroundUnits);
                    int dieRoll = Random.Range(1, 7);
                    combatRoll = new CombatRoll()
                    {
                        UID = _rollUIDTicker,
                        iDieRoll = dieRoll,
                        IsHit = dieRoll <= firingUnitType.GroundAttack,
                        iShooter = playerCadre.ID,
                        iTarget = target?.ID ?? -1
                    };
                    combatDiceDistribution.GroundDice--;
                }
                else if (combatDiceDistribution.AirDice > 0)
                {
                    target = PickTarget(opposingAirUnits);
                    int dieRoll = Random.Range(1, 7);
                    combatRoll = new CombatRoll()
                    {
                        UID = _rollUIDTicker,
                        iDieRoll = dieRoll, 
                        IsHit = dieRoll <= firingUnitType.AirAttack, 
                        iShooter = playerCadre.ID, 
                        iTarget = target?.ID ?? -1
                    };
                    combatDiceDistribution.AirDice--;
                }
                else if (combatDiceDistribution.SeaDice > 0)
                {
                    target = PickTarget(opposingSeaUnits);
                    int dieRoll = Random.Range(1, 7);
                    combatRoll = new CombatRoll()
                    {
                        UID = _rollUIDTicker,
                        iDieRoll = dieRoll, 
                        IsHit = dieRoll <= firingUnitType.SeaAttack,
                        iShooter = playerCadre.ID, 
                        iTarget = target?.ID ?? -1
                    };
                    combatDiceDistribution.SeaDice--;
                }
                else if (combatDiceDistribution.SubDice > 0)
                {
                    target = PickTarget(opposingSubUnits);
                    int dieRoll = Random.Range(1, 7);
                    combatRoll = new CombatRoll()
                    {
                        UID = _rollUIDTicker,
                        iDieRoll = dieRoll,
                        IsHit = dieRoll <= firingUnitType.SubAttack,
                        iShooter = playerCadre.ID,
                        iTarget = target?.ID ?? -1
                    };
                    combatDiceDistribution.SubDice--;
                }
                else throw new InvalidOperationException("Provided dice distribution does not have enough dice selected");

                _rollUIDTicker++;
                if (combatRoll.IsHit && target != null) target.TakeHit();
                CombatRolls.Add(combatRoll);
            }
            
            if (initiativeFiringQueue.TryDequeue(out int iNewPhasingPlayer))
            {
                PassToPlayerControl(units, iNewPhasingPlayer);
            }
            else
            {
                AdvanceInitiativeAndResolve();
            }
        }

        void PassToPlayerControl(GameCadre[] units, int iPhasingPlayer)
        {
            StageCounter++;
            this.iPhasingPlayer = iPhasingPlayer;
            DecidingDice = true;
            numDiceAvailable = CalculateAvailableDice(this.iPhasingPlayer, units);
            GameState.PushCombatState();
        }

        GameCadre PickTarget(List<GameCadre> potentialTargets)
        {
            
            switch (GameState.Ruleset.CombatDamageRule)
            {
                case CombatDamageRule.PreferHigherInitiativeEvenSpread:
                    GameCadre idealTarget = null;
                    foreach (var potentialTarget in potentialTargets)
                    {
                        if (idealTarget == null) idealTarget = potentialTarget;
                        else if (potentialTarget.Pips > idealTarget.Pips)
                            idealTarget = potentialTarget;
                        else if (potentialTarget.Pips == idealTarget.Pips &&
                                 potentialTarget.UnitType.IdAndInitiative < idealTarget.UnitType.IdAndInitiative)
                            idealTarget = potentialTarget;
                    }
                    return idealTarget;
                
                case CombatDamageRule.RandomEvenSpread:
                    List<GameCadre> validTargets = new List<GameCadre>();
                    int highestPips = int.MaxValue;
                    foreach (var potentialTarget in potentialTargets)
                    {
                        if (potentialTarget.Pips > highestPips)
                            highestPips = potentialTarget.Pips;
                    }
                    foreach (var potentialTarget in potentialTargets)
                    {
                        if (potentialTarget.Pips == highestPips)
                            validTargets.Add(potentialTarget);
                    }
                    return validTargets[Random.Range(0, validTargets.Count)];
                
                case CombatDamageRule.FullRandom:
                    return potentialTargets[Random.Range(0, potentialTargets.Count)];
                
                default: throw new NotImplementedException();
            }


            
        }

        int CalculateAvailableDice(int iPlayer, GameCadre[] allUnitsInCombat)
        {
            int dice = 0;
            foreach (var cadre in allUnitsInCombat)
            {
                if (cadre.Faction.ID == iPlayer && cadre.UnitType.IdAndInitiative == initiative && cadre.Pips > 0)
                {
                    dice += cadre.Pips;
                }
            }

            return dice;
        }

        void EndCombat()
        {
            Debug.Log("Ending combat");
            foreach (var cadre in GameState.GetEntitiesOfType<GameCadre>())
            {
                if (cadre is null) continue;
                if (cadre.Pips <= 0) 
                    cadre.Kill();
            }
            GameState.ActiveCombat = null;
            GameState.PushCombatState();
        }
        
        public void ReceiveFullState(TTGameState gameState, ref DataStreamReader incomingMessage)
        {
            GameState = gameState;
            CombatUID = incomingMessage.ReadInt();
            iTile = incomingMessage.ReadInt();
            iAttackerFaction = incomingMessage.ReadInt();
            iDefenderFaction = incomingMessage.ReadInt();
            initiative = incomingMessage.ReadInt();
            iPhasingPlayer = incomingMessage.ReadInt();
            numDiceAvailable = incomingMessage.ReadInt();
            StageCounter = incomingMessage.ReadInt();
            DecidingDice = incomingMessage.ReadByte() == 1;

            ProvidedCombatDiceDistribution = CombatDiceDistribution.Recreate(ref incomingMessage);
            
            int supportCadresLength = incomingMessage.ReadInt();
            iSupportingCadres = new int[supportCadresLength];
            for (int i = 0; i < supportCadresLength; i++)
            {
                iSupportingCadres[i] = incomingMessage.ReadInt();
            }
            
            int combatRollsCount = incomingMessage.ReadInt();
            CombatRolls = new List<CombatRoll>();
            for (int i = 0; i < combatRollsCount; i++)
            {
                var combatRoll = CombatRoll.Recreate(ref incomingMessage);
                CombatRolls.Add(combatRoll);
            }
        }

        public void WriteFullState(ref DataStreamWriter outgoingMessage)
        {
            outgoingMessage.WriteInt(CombatUID);
            outgoingMessage.WriteInt(iTile);
            outgoingMessage.WriteInt(iAttackerFaction);
            outgoingMessage.WriteInt(iDefenderFaction);
            outgoingMessage.WriteInt(initiative);
            outgoingMessage.WriteInt(iPhasingPlayer);
            outgoingMessage.WriteInt(numDiceAvailable);
            outgoingMessage.WriteInt(StageCounter);
            outgoingMessage.WriteByte((byte)(DecidingDice == true ? 1 : 0));
    
            ProvidedCombatDiceDistribution.Write(ref outgoingMessage);

            outgoingMessage.WriteInt(iSupportingCadres.Length);
            foreach (var value in iSupportingCadres)
            {
                outgoingMessage.WriteInt(value);
            }

            outgoingMessage.WriteInt(CombatRolls.Count);
            foreach (var roll in CombatRolls)
            {
                roll.Write(ref outgoingMessage);
            }
        }
        
        public int HashFullState(int asPlayer)
        {
            int hash = asPlayer;
            hash = CombineHashes(hash, CombatUID.GetHashCode());
            hash = CombineHashes(hash, iTile.GetHashCode());
            hash = CombineHashes(hash, iAttackerFaction.GetHashCode());
            hash = CombineHashes(hash, iDefenderFaction.GetHashCode());
            hash = CombineHashes(hash, initiative.GetHashCode());
            hash = CombineHashes(hash, iPhasingPlayer.GetHashCode());
            hash = CombineHashes(hash, DecidingDice.GetHashCode());
            hash = CombineHashes(hash, ProvidedCombatDiceDistribution.GetHashCode());

            foreach (var cadre in iSupportingCadres)
            {
                hash = CombineHashes(hash, cadre);
            }

            if (CombatRolls != null)
            {
                foreach (var roll in CombatRolls)
                {
                    hash = CombineHashes(hash, roll.GetHashCode());
                }
            }

            return hash;
        }

        private int CombineHashes(int hash, int value)
        {
            unchecked
            {
                return hash + (value * 17);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is GameCombat combat)
            {
                if (combat.iTile == iTile &&
                    combat.iAttackerFaction == iAttackerFaction &&
                    combat.iDefenderFaction == iDefenderFaction &&
                    combat.CombatUID == CombatUID) return true;
            }

            return false;
        }
    }
}