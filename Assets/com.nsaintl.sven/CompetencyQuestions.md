# SVEN's Competency Questions

## Prefixes for SPARQL queries

```sparql
PREFIX : <https://sven.lisn.upsaclay.fr/ve/Buffer/>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
PREFIX sven: <https://sven.lisn.upsaclay.fr/ontology#>
PREFIX time: <http://www.w3.org/2006/time#>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX ofn: <http://www.ontotext.com/sparql/functions/>
```

## Spatial reasoning

### Which objects are present in the scene at a given moment?

```sparql
SELECT DISTINCT ?object ?objectName
WHERE {
    ?object a sven:VirtualObject ;
    		sven:hasTemporalExtent ?interval ;
    		rdfs:label ?objectName .

    BIND("2025-12-04T11:57:41.400+01:00"^^xsd:dateTime AS ?instantTime)
    ?interval a time:Interval ;
    		  time:hasBeginning/time:inXSDDateTime ?startTime .
    OPTIONAL {
        ?interval time:hasEnd/time:inXSDDateTime ?_endTime .
    }
    BIND(IF(BOUND(?_endTime), ?_endTime, NOW()) AS ?endTime)
    FILTER(?startTime <= ?instantTime && ?instantTime < ?endTime)
}
```

### Which of these objects are visible, reachable, or occluded from the user’s point of view?

```sparql
SELECT DISTINCT ?object ?objectName ?visible ?reachable
WHERE {
    BIND("2025-12-04T11:57:41.400+01:00"^^xsd:dateTime AS ?instantTime)

    ?object a sven:VirtualObject ;
            sven:hasTemporalExtent ?objectInterval ;
            rdfs:label ?objectName .

    ?objectInterval a time:Interval ;
    				time:hasBeginning/time:inXSDDateTime ?objectStartTime .
    OPTIONAL {
        ?objectInterval time:hasEnd/time:inXSDDateTime ?_objectEndTime .
    }
    BIND(IF(BOUND(?_objectEndTime), ?_objectEndTime, NOW()) AS ?objectEndTime)
    FILTER(?objectStartTime <= ?instantTime && ?instantTime < ?objectEndTime)

    OPTIONAL{
        ?user a sven:User ;
              sven:pointOfView ?pov .
        ?lookEvent a sven:Event ;
                   sven:sender ?pov ;
                   sven:receiver ?object ;
                   sven:hasTemporalExtent ?visibleInterval .

        ?visibleInterval a time:Interval ;
                        time:hasBeginning/time:inXSDDateTime ?visibleStartTime .
        OPTIONAL {
            ?visibleInterval time:hasEnd/time:inXSDDateTime ?_visibleEndTime .
        }
        BIND(IF(BOUND(?_visibleEndTime), ?_visibleEndTime, NOW()) AS ?visibleEndTime)
        FILTER(?visibleStartTime <= ?instantTime && ?instantTime < ?visibleEndTime)
    }
    BIND(BOUND(?user) AS ?visible)

    OPTIONAL{
        ?user a sven:User ;
              sven:graspArea ?graspArea .
        ?lookEvent a sven:Event ;
                   sven:sender ?graspArea ;
                   sven:receiver ?object ;
                   sven:hasTemporalExtent ?reachableInterval .

        ?reachableInterval a time:Interval ;
                           time:hasBeginning/time:inXSDDateTime ?reachableStartTime .
        OPTIONAL {
            ?reachableInterval time:hasEnd/time:inXSDDateTime ?_reachableEndTime .
        }
        BIND(IF(BOUND(?_reachableEndTime), ?_reachableEndTime, NOW()) AS ?reachableEndTime)
        FILTER(?reachableStartTime <= ?instantTime && ?instantTime < ?reachableEndTime)
    }

    BIND(BOUND(?graspArea) AS ?reachable)
}
```

### What spatial relationships (e.g., adjacency, containment, distance) exist between objects in the environment?

...

## Temporal reasoning

### How do object states or properties (e.g., position, color, or activation) evolve throughout an experiment?

```sparql
SELECT ?object ?objectName ?x ?y ?z
WHERE {
    BIND("Interactable Banana 2" AS ?objectName)
    ?object a sven:VirtualObject ;
    		rdfs:label ?objectName ;
    		sven:component ?component .
    ?component sven:position ?position .
    ?position sven:hasTemporalExtent ?interval ;
    		  sven:x ?x ;
    		  sven:y ?y ;
    		  sven:z ?z .
    ?interval time:before ?before ;
    		  time:after ?after ;
    		  time:hasBeginning/time:inXSDDateTime ?time .
} ORDER BY ?time
```

### What sequence of actions or events occurs before or after a given instant?

Todo pas trop compliqué.

### During which intervals are specific objects or users active?

...

## Interaction analysis

### What type of food did the user look at the most?

```sparql
SELECT DISTINCT ?objectType (SUM(?totalSeconds) AS ?sumSeconds)
WHERE {
    ?user a sven:User ;
          sven:pointOfView ?pov .
    ?lookEvent a sven:Event ;
               sven:sender ?pov ;
               sven:receiver ?lookedObject ;
               sven:hasTemporalExtent/time:hasXSDDuration ?duration .
    ?lookedObject a ?objectType .
    ?objectType rdfs:subClassOf sven:Food .
    BIND(ofn:asMillis(?duration) / 1000 AS ?totalSeconds)
}
GROUP BY ?objectType
ORDER BY DESC(?sumSeconds)
```

### Which interactions—such as collisions, grasping, or pointing—take place within a specified time window?

### Which entities participate in these interactions, and how frequently do they occur?

### How are multimodal input events (e.g., gaze, motion, speech) combined to achieve specific outcomes?

## User behaviour interpretation

### Which objects receive the most visual attention or engagement?

### Which categories of domain-specific objects (e.g., tools, fruits, medical items) elicit the highest interaction rates?

### How do user actions correlate with contextual or environmental factors, such as task difficulty or spatial layout?
