# Semantized Virtual ENvironment (SVEN) for Unity

[![Unity 6000.0+](https://img.shields.io/badge/unity-6000.0%2B-blue.svg)](https://unity3d.com/get-unity/download)

<!---[![openupm](https://img.shields.io/npm/v/com.dbrizov.naughtyattributes?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.dbrizov.naughtyattributes/)-->
<!---[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](https://github.com/dbrizov/NaughtyAttributes/blob/master/LICENSE)-->

SVEN is a Unity package that allows you to semantize your Virtual Environment (VE) into a knowledge graph, making it possible to perform complex queries on your VE during the experience, as well as the ability to replay and analyze it afterwards.

It is based on [Semantic Web](https://en.wikipedia.org/wiki/Semantic_Web) and [Linked Data](https://en.wikipedia.org/wiki/Linked_data) technologies and uses the [SPARQL](https://en.wikipedia.org/wiki/SPARQL) query language to query the data. Using such technology enriches your virtual experience in several ways:

1. **Rule-based reasoning**: By using semantic rules, you can infer new information from existing data, especially if you have a complex domain or incomplete data.
2. **Data interoperability**: By using open standards and linked data, SVEN facilitates the integration and exchange of data between different systems and platforms.
3. **Computation delegation**: SVEN allows you to delegate complex computations to a remote server, saving local resources, which is particularly beneficial for VR/AR applications where resources are often limited.

## System Requirements

Unity **6000.0** or later versions.

## Installation

1. The package is available in **Releases** section. You can download the latest version from [here](https://gitlab.lisn.upsaclay.fr/nsaintl/SVENUnity/-/releases/permalink/latest), then import it into your Unity project.

2. You can also install via git url by adding this entry in your **manifest.json**

```
"com.***.***": "https://gitlab.lisn.upsaclay.fr/nsaintl/SVENUnity.git#upm"
```

If you don't have openUPM CLI, you will need to install it first by following the instructions [here](https://openupm.com/docs/getting-started.html), or by adding this entry in your **manifest.json**. This step is important, otherwise the dependencies will not install properly.

```json
{
    "scopedRegistries": [
        {
            "name": "package.openupm.com",
            "url": "https://package.openupm.com",
            "scopes": ["com.gamesoft.dotween", "com.dbrizov.naughtyattributes"],
            "overrideBuiltIns": false
        }
    ]
}
```

## Support

This project is developed as part of a PhD thesis at the LISN laboratory of the University of Paris-Saclay. For any questions, you can contact [Nicolas Saint-Léger](mailto:nicolas.saint-leger@universite-paris-saclay.fr).

# Overview

## Semantize your Virtual ENvironment (VE) _(write your knowledge graph)_

Pour créer un environnement virtuel semantisé, il suffit de suivre les étapes suivantes :

1. Ajouter dans votre scène un composant **GraphBuffer** (GameObject > Semantic > GraphBuffer) qui permet de stocker les données de votre environnement virtuel sous forme de graphe de connaissance et de les envoyer à un serveur distant (ou un fichier local) pour pouvoir l'analyser ou le rejouer plus tard.

![Instantiate GraphBuffer](./Assets/com.nsaintl.sven/Documentation~/instantiate_graphbuffer.png)

2. Configurer le composant **GraphBuffer** en fonction de vos besoins :

| Propriété                | Description                                                                                                                           |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------- |
| **Endpoint**             | L'URL du serveur distant où les données seront envoyées (ou le chemin du fichier local où les données seront sauvegardées)            |
| **Storage Name**         | Le nom du graphe de connaissance qui sera créé sur le serveur distant (ou le nom du fichier local où les données seront sauvegardées) |
| **Instant Per Second**   | La fréquence à laquelle les données de l'environnement virtuel seront vérifiées et sauvegardées dans le graphe de connaissance        |
| **Ontology Description** | La description de l'ontologie utilisée pour décrire les données de l'environnement virtuel                                            |

![GraphBuffer](./Assets/com.nsaintl.sven/Documentation~/graphbuffer.png)

3. Créer dans votre projet une ressource **OntologyDescription** (Create > Semantic > OntologyDescription) qui décrit permet de décrire la configuration de l'ontologie utilisée pour décrire les données de l'environnement virtuel.

![Instantiate OntologyDescription](./Assets/com.nsaintl.sven/Documentation~/instantiate_ontologydescription.png)

4. Configurer la ressource **OntologyDescription** en fonction de vos besoins :

| Propriété         | Description                                                                                                                |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **Name**          | Nom de l'ontologie utilisée pour décrire les données de l'environnement virtuel                                            |
| **Base Uri**      | L'URI de base utilisée pour décrire les données de l'environnement virtuel                                                 |
| **Namespaces**    | Les espaces de noms utilisés pour décrire les données de l'environnement virtuel                                           |
| **Ontology File** | Le fichier **_.ttl_** contenant la description de l'ontologie utilisée pour décrire les données de l'environnement virtuel |

![OntologyDescription](./Assets/com.nsaintl.sven/Documentation~/ontologydescription.png)

5. Ajouter à chaque objet que vous souhaitez semantiser un composant **SemantizationCore** (GameObject > Semantic > SemantizationCore) qui permet de scanner les composants, vous permettant de choisir les propriétés à observer et à modifier. Vous pouvez également choisir si le composant doit être observé dynamiquement ou s'il est considéré comme statique et sémantisé uniquement à sa création.

![Semantization Core](./Assets/com.nsaintl.sven/Documentation~/semantizationcore.png)

6. Votre scène est maintenant prête à être semantisée. Vous pouvez maintenant lancer votre application et observer les données de votre environnement virtuel être sauvegardées dans le graphe de connaissance à la fin de votre expérience.

Les fichiers **_.ttl_** sont sauvegardés dans le dossier **SVENs** à la racine de votre projet.

## Replay your Semantize Virtual ENvironment (SVEN) _(read your knowledge graph)_

![GraphReader Remote](./Assets/com.nsaintl.sven/Documentation~/graphreader_remote.png)
![GraphReader Local](./Assets/com.nsaintl.sven/Documentation~/graphreader_local.png)

![Reader Scene](./Assets/com.nsaintl.sven/Documentation~/reader_scene.png)

### Local

// toucher au composant **GraphReader** mettre en local -> charger un fichier **_.ttl_**

### Remote

## How to support new components ?

To support non-generic components, you can add their descriptions to the `MapppedComponents` dictionary. For example, to add a description for the `Atom` component, you can use the following code:

```csharp
MapppedComponents.AddComponentDescription(typeof(Atom), new("Atom",
    new List<Delegate>
    {
        (Func<Atom, PropertyDescription>)(atom => new PropertyDescription("enabled", () => atom, value => atom.enabled = value.ToString() == "true", 1)),
        (Func<Atom, PropertyDescription>)(atom => new PropertyDescription("atomType", () => atom, value => atom.type = value.ToString(), 1)),
    }));
```

This code snippet maps the `Atom` component to its properties, allowing SVEN to semantize and interact with it properly. It also enables custom getters and setters for observing their properties. Make sure to call this at the beginning of the scene to ensure everything works correctly.
