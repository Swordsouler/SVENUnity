// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.OwlTime;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Sven.GraphManagement.Description
{
    /// <summary>
    /// Graph that contains the scene content.
    /// </summary>        
    public class SceneContent
    {
        /// <summary>
        /// Instant of the scene content.
        /// </summary> 
        public Instant Instant { get; set; }
        /// <summary>
        /// Scene content dictionary.
        /// </summary>
        public Dictionary<string, GameObjectDescription> GameObjects { get; set; }

        public SceneContent()
        {
            GameObjects = new();
        }

        public SceneContent(Instant instant)
        {
            Instant = instant;
            GameObjects = new();
        }

        public SceneContent(Dictionary<string, GameObjectDescription> gameObjects)
        {
            GameObjects = gameObjects;
        }

        /// <summary>
        /// ToString method.
        /// </summary>
        /// <returns>String representation of the property description.</returns>
        public override string ToString()
        {
            return $"{Instant?.inXSDDateTime}\n{string.Join($"\n", GameObjects.Select(x => $"---------- {x.Key} ({x.Value.Name}) ----------\n{x.Value}"))}";
        }
    }
}
