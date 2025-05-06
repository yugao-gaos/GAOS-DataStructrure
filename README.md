# GAOS DataContainer

A Unity package for creating, managing, and serializing hierarchical data containers.

## Features

- Generic data container that can hold various types of data
- Supports primitive types, arrays, dictionaries, and nested DataContainers
- Serialization/deserialization to/from JSON
- Deep copy functionality
- DataStructure ScriptableObject for designing data templates
- DataInstance ScriptableObject for runtime data instances
- Custom editor for easy data editing

## Installation

1. Open the Unity Package Manager
2. Click the "+" button and select "Add package from git URL..."
3. Enter the repository URL: `https://github.com/gaos/com.gaos.datacontainer.git`
4. Click "Add"

## Usage

### Creating a DataStructure

```csharp
// Create a new DataStructure asset
var playerStructure = ScriptableObject.CreateInstance<DataStructure>();

// Define the structure
var container = playerStructure.Container;
container.Set("name", "Default Player");
container.Set("level", 1);
container.Set("health", 100f);

// Create a nested container
var statsContainer = container.GetOrCreateContainer("stats");
statsContainer.Set("strength", 10);
statsContainer.Set("agility", 10);
statsContainer.Set("intelligence", 10);
```

### Creating a DataInstance

```csharp
// Load a DataStructure
var playerStructure = Resources.Load<DataStructure>("PlayerStructure");

// Create a runtime instance
var playerInstance = playerStructure.CreateInstance("Player1");

// Modify values (not structure)
playerInstance.Container.Set("name", "Hero");
playerInstance.Container.Set("level", 5);
```

## Requirements

- Unity 2019.4 or later
- GAOS.ServiceLocator package 