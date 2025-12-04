# SVEN's Competency Questions

## Spatial reasoning

### Which objects are present in the scene at a given moment?

### Which of these objects are visible, reachable, or occluded from the user’s point of view?

### What spatial relationships (e.g., adjacency, containment, distance) exist between objects in the environment?

## Temporal reasoning

### How do object states or properties (e.g., position, color, or activation) evolve throughout an experiment?

### What sequence of actions or events occurs before or after a given instant?

### During which intervals are specific objects or users active?

## Interaction analysis

### What type of food did the user look at the most?

```sparql
SELECT ?objectType (SUM(?totalSeconds) AS ?sumSeconds)
WHERE {
    ?user a sven:User ;
          sven:pointOfView ?pov .
    ?lookEvent a sven:Event ;
               sven:sender ?pov ;
               sven:receiver ?lookedObject ;
               time:hasTemporalExtent/time:hasXSDDuration ?duration .
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
