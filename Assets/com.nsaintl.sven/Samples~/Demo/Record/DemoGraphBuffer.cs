// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Sven.Content;
using Sven.GraphManagement;

namespace Sven.Demo
{
    public class DemoGraphBuffer : GraphBuffer
    {
        public new void Awake()
        {
            graphName = DemoManager.graphName;
            endpoint = DemoManager.EndpointUri.ToString() + "/rdf-graphs/service";
            instantPerSecond = DemoManager.semantisationFrequency;
            MapppedComponents.AddComponentDescription(typeof(DemoSprayController), new("Spray",
                new List<Delegate>
                {
                    (Func<DemoSprayController, MapppedComponents.PropertyDescription>)(spray => new MapppedComponents.PropertyDescription("enabled", () => spray.enabled, value => spray.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Pumpkin), new("PumpkinComponent",
                new List<Delegate>
                {
                    (Func<Pumpkin, MapppedComponents.PropertyDescription>)(pumpkin => new MapppedComponents.PropertyDescription("enabled", () => pumpkin.enabled, value => pumpkin.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Apple), new("AppleComponent",
                new List<Delegate>
                {
                    (Func<Apple, MapppedComponents.PropertyDescription>)(apple => new MapppedComponents.PropertyDescription("enabled", () => apple.enabled, value => apple.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Banana), new("BananaComponent",
                new List<Delegate>
                {
                    (Func<Banana, MapppedComponents.PropertyDescription>)(banana => new MapppedComponents.PropertyDescription("enabled", () => banana.enabled, value => banana.enabled = value.ToString() == "true", 1)),
                }, 1));
            MapppedComponents.AddComponentDescription(typeof(Carrot), new("CarrotComponent",
                new List<Delegate>
                {
                    (Func<Carrot, MapppedComponents.PropertyDescription>)(carrot => new MapppedComponents.PropertyDescription("enabled", () => carrot.enabled, value => carrot.enabled = value.ToString() == "true", 1)),
                }, 1));
            base.Awake();
        }
    }
}
