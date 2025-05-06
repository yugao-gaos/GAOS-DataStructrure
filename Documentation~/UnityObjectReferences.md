# Unity Object References in DataStructure

The DataStructure system now supports direct references to Unity objects (GameObject, Sprite, Material, ScriptableObject, etc.) within your data structures. This document explains how this system works and how to use it.

## How It Works

Unity's serialization system normally doesn't allow custom serialization of UnityEngine.Object references. To overcome this limitation, the DataContainer system uses a special internal reference tracking system:

1. When you set a UnityEngine.Object reference, it's stored in a serialized list inside the DataContainer
2. A reference wrapper is created to track the reference path
3. When you retrieve the reference, the system automatically resolves it from the internal tracking system

The best part is, you don't need to do anything special to use this system - the existing Set/Get and PathSet/PathGet methods handle everything automatically.

## Supported Types

Any type that derives from UnityEngine.Object can be stored in the DataContainer:

- GameObject
- Prefabs 
- Sprite
- Texture2D
- Material
- AudioClip
- ScriptableObject (including custom types)
- DataInstance (for nesting data instances)
- And any other UnityEngine.Object derived type

## Usage Examples

```csharp
// Setting references
var container = new DataContainer();

// Simple set/get
container.Set("playerPrefab", myPlayerPrefab);
var prefab = container.Get<GameObject>("playerPrefab");

// Path-based set/get
container.PathSet("ui.icons.player", playerSprite);
var sprite = container.PathGet<Sprite>("ui.icons.player");

// Custom ScriptableObject references
container.Set("gameSettings", mySettingsObject);
var settings = container.Get<GameSettings>("gameSettings");
```

## Editor Support

The DataStructure editor window has been updated with additional type options:

- Common Unity types are available in the type dropdown when adding new properties
- The appropriate ObjectField is used when editing Unity object properties
- Reference integrity is maintained when copying or duplicating DataStructures

## Serialization Notes

- Unity Object references are properly serialized when the container is part of a ScriptableObject
- When using ToJson/FromJson, reference data is included, but actual object references need to be restored

## Best Practices

1. For scene-independent data, use ScriptableObjects or prefabs as references 
2. For scene-specific objects, be aware that references might need to be reset when changing scenes
3. When creating DataInstances, all UnityEngine.Object references are maintained in the instance

## Technical Details

For those interested in the implementation details, the system uses:

- A serialized list of UnityEngine.Object references and their paths
- An internal UnityObjectReference<T> class to track references
- Special handling in the DataContainer's Get/Set methods to automatically resolve references 