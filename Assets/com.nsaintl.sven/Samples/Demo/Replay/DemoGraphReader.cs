// Copyright (c) 2025 CNRS, LISN – Université Paris-Saclay
// Author: Nicolas SAINT-LÉGER
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Sven.GraphManagement;
using Sven.Utils;
using System.Threading.Tasks;

namespace Sven.Demo
{
    public class DemoGraphReader : GraphController
    {
        public async Task<string> GetTTL()
        {
            return await GraphManager.DownloadTTLFromEndpoint(SvenSettings.EndpointUrl);
        }
    }
}
