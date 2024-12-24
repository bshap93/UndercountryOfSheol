﻿using System;
using System.Collections.Generic;
using MoreMountains.InventoryEngine;

namespace Project.Gameplay.Player.Inventory.Models
{
    [Serializable]
    public class Inventory
    {
        public List<InventoryItem> items = new();
        public int maxSlots = 20;

        public List<InventoryItem> GetContents()
        {
            return new List<InventoryItem>(items);
        }
    }
}
