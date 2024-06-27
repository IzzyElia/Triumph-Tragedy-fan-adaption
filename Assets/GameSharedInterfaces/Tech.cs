using System;
using UnityEngine;

namespace GameSharedInterfaces
{

    [Serializable] public class Tech
    {
        public string Name;
        [NonSerialized] public Texture2D Icon;
        [NonSerialized] public Sprite Sprite;
        public int ID;

        public Tech(string Name)
        {
            this.Name = Name;
        }
        
        public Tech SetGraphics()
        {
            Icon = Resources.Load<Texture2D>($"Icons/Techs/{Name}");
            if (Icon is null)
            {
                Debug.LogWarning($"No icon found for tech {Name}");
                Icon = Resources.Load<Texture2D>("Icons/Techs/fallback");
            }
            Sprite = Resources.Load<Sprite>($"Icons/Techs/{Name}");
            if (Sprite is null)
            {
                Debug.LogWarning($"No icon found for tech {Name}");
                Sprite = Resources.Load<Sprite>("Icons/Techs/fallback");
            }

            return this;
        }
    }
}