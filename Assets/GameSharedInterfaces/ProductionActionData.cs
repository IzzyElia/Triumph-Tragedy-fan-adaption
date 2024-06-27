using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace GameSharedInterfaces
{
    public enum ProductionActionType
    {
        Error,
        BuildUnit,
        ReinforceUnit,
        DrawCard,
    }

    public struct ProductionActionData
    {
        public ProductionActionType ActionType;
        public int iCadre; //If applicable
        public int iUnitType; //If applicable
        public int iTile; //If applicable
        public CardType CardType;

        public static ProductionActionData BuildUnitAction(int iTile, int iUnitType)
        {
            return new ProductionActionData(ProductionActionType.BuildUnit, iTile:iTile, iUnitType:iUnitType);
        }
        
        public static ProductionActionData ReinforceUnitAction(int iCadre)
        {
            return new ProductionActionData(ProductionActionType.ReinforceUnit, iCadre: iCadre);
        }
        
        public static ProductionActionData DrawCardAction(CardType cardType)
        {
            return new ProductionActionData(ProductionActionType.DrawCard, cardType:cardType);
        }

        public static ProductionActionData Recreate(ref DataStreamReader message)
        {
            ProductionActionType actionType = (ProductionActionType)message.ReadByte();
            try
            {
                switch (actionType)
                {
                    case ProductionActionType.BuildUnit:
                        int iTile = message.ReadInt();
                        int iUnitType = message.ReadByte();
                        return new ProductionActionData(actionType, iTile: iTile, iUnitType:(int)iUnitType);
                    case ProductionActionType.ReinforceUnit:
                        int iCadre = message.ReadInt();
                        return new ProductionActionData(actionType, iCadre: iCadre);
                    case ProductionActionType.DrawCard:
                        CardType cardType = (CardType)message.ReadByte();
                        return new ProductionActionData(actionType, cardType: cardType);
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return new ProductionActionData(ProductionActionType.Error);
            }

        }

        public void Write(ref DataStreamWriter message)
        {
            message.WriteByte((byte)ActionType);
            switch (this.ActionType)
            {
                case ProductionActionType.BuildUnit:
                    message.WriteInt(iTile);
                    message.WriteByte((byte)iUnitType);
                    break;
                case ProductionActionType.ReinforceUnit:
                    message.WriteInt(iCadre);
                    break;
                case ProductionActionType.DrawCard:
                    message.WriteByte((byte)CardType);
                    break;
                default: throw new NotImplementedException();
            }
        }
        
        private ProductionActionData(ProductionActionType actionType, int iCadre = -1, int iTile = -1, CardType cardType = default, int iUnitType = -1)
        {
            this.ActionType = actionType;
            this.iCadre = iCadre;
            this.iTile = iTile;
            this.CardType = cardType;
            this.iUnitType = iUnitType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActionType, iCadre, iTile, CardType, iUnitType);
        }
    }
}