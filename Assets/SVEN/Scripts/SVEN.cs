using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using UnityEngine;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace VDS.RDF.Parsing
{
    public static class SvenSpecsHelper
    {
        public static readonly System.Uri baseUri = UriFactory.Create("http://example.org/");
        public static readonly System.Uri GameObject = UriFactory.Create(baseUri + "GameObject");
    }
}