using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameSharedInterfaces.Triumph_and_Tragedy
{
    [Serializable]
    public struct UnitAIScore
    {
        /// <summary>
        /// What is the units base effectiveness on the ground
        /// </summary>
        public float groundCombatScore;
        /// <summary>
        /// What is the units base effectiveness against air
        /// </summary>
        public float airCombatScore;
        /// <summary>
        /// What is the units base effectiveness at sea
        /// </summary>
        public float seaCombatScore;
        /// <summary>
        /// How easily can the unit be relocated 
        /// </summary>
        public float mobilityFactor;
        /// <summary>
        /// How effective is the unit for the taking and holding of land (usually 0 or 1)
        /// </summary>
        public float landCaptureFactor;
        /// <summary>
        /// How much damage is the unit expected to land during navel invasions before the enemy fires back
        /// </summary>
        public float navelInvasionShockPower;
        /// <summary>
        /// How useful is the unit for ground encirclements and blockades
        /// </summary>
        public float groundInterdictionScore;
        /// <summary>
        /// How useful is the unit for navel encirclements and blockades
        /// </summary>
        public float navelInterdictionScore;

        public float Lambda(UnitAIScore target)
        {
            UnitAIScore factored = FactorOf(target);
            // min of all fields
            float min = float.PositiveInfinity;
            // Repeat this for all fields
            if (factored.groundCombatScore < min) min = factored.groundCombatScore;
            if (factored.airCombatScore < min) min = factored.airCombatScore;
            if (factored.seaCombatScore < min) min = factored.seaCombatScore;
            if (factored.mobilityFactor < min) min = factored.mobilityFactor;
            if (factored.landCaptureFactor < min) min = factored.landCaptureFactor;
            if (factored.navelInvasionShockPower < min) min = factored.navelInvasionShockPower;
            if (factored.groundInterdictionScore < min) min = factored.groundInterdictionScore;
            if (factored.navelInterdictionScore < min) min = factored.navelInterdictionScore;

            return min;
        }

        /// <summary>
        /// Returns a new AIScore with the average of this and b's value
        /// </summary>
        public UnitAIScore AverageOf(UnitAIScore b)
        {
            return new UnitAIScore
            {
                groundCombatScore = (this.groundCombatScore + b.groundCombatScore) / 2f,
                airCombatScore = (this.airCombatScore + b.airCombatScore) / 2f,
                seaCombatScore = (this.seaCombatScore + b.seaCombatScore) / 2f,
                mobilityFactor = (this.mobilityFactor + b.mobilityFactor) / 2f,
                landCaptureFactor = (this.landCaptureFactor + b.landCaptureFactor) / 2f,
                navelInvasionShockPower = (this.navelInvasionShockPower + b.navelInvasionShockPower) / 2f,
                groundInterdictionScore = (this.groundInterdictionScore + b.groundInterdictionScore) / 2f,
                navelInterdictionScore = (this.navelInterdictionScore + b.navelInterdictionScore) / 2f
            };
        }
        
        /// <summary>
        /// Returns a new AIScore with the max of each value
        /// </summary>
        public UnitAIScore MaxOf(UnitAIScore b)
        {
            // max of each field
            return new UnitAIScore
            {
                groundCombatScore = Mathf.Max(this.groundCombatScore, b.groundCombatScore),
                airCombatScore = Mathf.Max(this.airCombatScore, b.airCombatScore),
                seaCombatScore = Mathf.Max(this.seaCombatScore, b.seaCombatScore),
                mobilityFactor = Mathf.Max(this.mobilityFactor, b.mobilityFactor),
                landCaptureFactor = Mathf.Max(this.landCaptureFactor, b.landCaptureFactor),
                navelInvasionShockPower = Mathf.Max(this.navelInvasionShockPower, b.navelInvasionShockPower),
                groundInterdictionScore = Mathf.Max(this.groundInterdictionScore, b.groundInterdictionScore),
                navelInterdictionScore = Mathf.Max(this.navelInterdictionScore, b.navelInterdictionScore)
            };
        }
        
        public UnitAIScore FactorOf(UnitAIScore b)
        {
            return new UnitAIScore
            {
                groundCombatScore = b.groundCombatScore <= 0 ? float.PositiveInfinity : this.groundCombatScore / b.groundCombatScore,
                airCombatScore = b.airCombatScore <= 0 ? float.PositiveInfinity : this.airCombatScore / b.airCombatScore,
                seaCombatScore = b.seaCombatScore <= 0 ? float.PositiveInfinity : this.seaCombatScore / b.seaCombatScore,
                mobilityFactor = b.mobilityFactor <= 0 ? float.PositiveInfinity : this.mobilityFactor / b.mobilityFactor,
                landCaptureFactor = b.landCaptureFactor <= 0 ? float.PositiveInfinity : this.landCaptureFactor / b.landCaptureFactor,
                navelInvasionShockPower = b.navelInvasionShockPower <= 0 ? float.PositiveInfinity : this.navelInvasionShockPower / b.navelInvasionShockPower,
                groundInterdictionScore = b.groundInterdictionScore <= 0 ? float.PositiveInfinity : this.groundInterdictionScore / b.groundInterdictionScore,
                navelInterdictionScore = b.navelInterdictionScore <= 0 ? float.PositiveInfinity : this.navelInterdictionScore / b.navelInterdictionScore,
            };
        }
        
        public UnitAIScore FactorOfCapped(UnitAIScore b)
        {
            return new UnitAIScore
            {
                groundCombatScore = Mathf.Max(b.groundCombatScore <= 0 ? float.PositiveInfinity : this.groundCombatScore / b.groundCombatScore, 1f),
                airCombatScore = Mathf.Max(b.airCombatScore <= 0 ? float.PositiveInfinity : this.airCombatScore / b.airCombatScore, 1f),
                seaCombatScore = Mathf.Max(b.seaCombatScore <= 0 ? float.PositiveInfinity : this.seaCombatScore / b.seaCombatScore, 1f),
                mobilityFactor = Mathf.Max(b.mobilityFactor <= 0 ? float.PositiveInfinity : this.mobilityFactor / b.mobilityFactor, 1f),
                landCaptureFactor = Mathf.Max(b.landCaptureFactor <= 0 ? float.PositiveInfinity : this.landCaptureFactor / b.landCaptureFactor, 1f),
                navelInvasionShockPower = Mathf.Max(b.navelInvasionShockPower <= 0 ? float.PositiveInfinity : this.navelInvasionShockPower / b.navelInvasionShockPower, 1f),
                groundInterdictionScore = Mathf.Max(b.groundInterdictionScore <= 0 ? float.PositiveInfinity : this.groundInterdictionScore / b.groundInterdictionScore, 1f),
                navelInterdictionScore = Mathf.Max(b.navelInterdictionScore <= 0 ? float.PositiveInfinity : this.navelInterdictionScore / b.navelInterdictionScore, 1f),
            };
        }
        
        public static UnitAIScore operator +(UnitAIScore a, UnitAIScore b)
        {
            return new UnitAIScore
            {
                groundCombatScore = a.groundCombatScore + b.groundCombatScore,
                airCombatScore = a.airCombatScore + b.airCombatScore,
                seaCombatScore = a.seaCombatScore + b.seaCombatScore,
                mobilityFactor = (a.mobilityFactor + b.mobilityFactor) / 2f,
                landCaptureFactor = (a.landCaptureFactor + b.landCaptureFactor) / 2f,
                navelInvasionShockPower = a.navelInvasionShockPower + b.navelInvasionShockPower,
                groundInterdictionScore = a.groundInterdictionScore + b.groundInterdictionScore,
                navelInterdictionScore = a.navelInterdictionScore + b.navelInterdictionScore
            };
        }

        /* how do we want to handle mobility factor and land capture factor?
        public static UnitAIScore operator *(UnitAIScore a, UnitAIScore b)
        {
            return new UnitAIScore
            {
                groundCombatScore = a.groundCombatScore * b.groundCombatScore,
                airCombatScore = a.airCombatScore * b.airCombatScore,
                seaCombatScore = a.seaCombatScore * b.seaCombatScore,
                mobilityFactor = (a.mobilityFactor + b.mobilityFactor) / 2f,
                landCaptureFactor = (a.landCaptureFactor + b.landCaptureFactor) / 2f,
                navelInvasionShockPower = a.navelInvasionShockPower * b.navelInvasionShockPower,
                groundInterdictionScore = a.groundInterdictionScore * b.groundInterdictionScore,
                navelInterdictionScore = a.navelInterdictionScore * b.navelInterdictionScore
            };
        }
        */
        
        public static UnitAIScore operator *(UnitAIScore a, float num)
        {
            return new UnitAIScore
            {
                groundCombatScore = a.groundCombatScore * num,
                airCombatScore = a.airCombatScore * num,
                seaCombatScore = a.seaCombatScore * num,
                mobilityFactor = a.mobilityFactor,
                landCaptureFactor = a.landCaptureFactor * num,
                navelInvasionShockPower = a.navelInvasionShockPower * num,
                groundInterdictionScore = a.groundInterdictionScore * num,
                navelInterdictionScore = a.navelInterdictionScore * num
            };
        }
    }
}