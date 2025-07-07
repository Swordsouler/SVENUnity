// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.Content;
using System;
using System.Collections.Generic;

namespace Sven.Demo
{
    public class DemoGraphLoader
    {
        public void Load()
        {
            MapppedComponents.AddComponentDescription(typeof(DemoSprayController), new("Spray",
                new List<Delegate>
                {
                    (Func<DemoSprayController, ComponentProperty>)(spray => new ComponentProperty("enabled", () => spray.enabled, value => spray.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Pumpkin), new("PumpkinComponent",
                new List<Delegate>
                {
                    (Func<Pumpkin, ComponentProperty>)(pumpkin => new ComponentProperty("enabled", () => pumpkin.enabled, value => pumpkin.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Apple), new("AppleComponent",
                new List<Delegate>
                {
                    (Func<Apple, ComponentProperty>)(apple => new ComponentProperty("enabled", () => apple.enabled, value => apple.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Banana), new("BananaComponent",
                new List<Delegate>
                {
                    (Func<Banana, ComponentProperty>)(banana => new ComponentProperty("enabled", () => banana.enabled, value => banana.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Carrot), new("CarrotComponent",
                new List<Delegate>
                {
                    (Func<Carrot, ComponentProperty>)(carrot => new ComponentProperty("enabled", () => carrot.enabled, value => carrot.enabled = value.ToString() == "true", 1)),
                }, 1));
        }
    }
}
