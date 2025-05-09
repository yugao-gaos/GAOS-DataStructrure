# DataStructure Code Generation Feature Reimplementation Plan

## Overview
This document outlines the step-by-step plan to reimplement the strongly-typed DataInstance code generation system for the GAOS DataStructure package. The goal is to automate the generation of interfaces and concrete DataInstance subclasses based on DataStructure assets, with robust Unity Editor integration and user control over which properties are included.

---

## 1. Data Model Preparation
- [x] Ensure `DataStructure` and `DataInstance` are abstracted for codegen (e.g., DataInstance is abstract, DataStructure exposes all paths and types).
- [x] Add a serialized list to `DataStructure` for tracking which property paths should be included in the generated interface (e.g., `List<string> InterfacePropertyPaths`).

## 2. Code Generation Core
- [x] Create a `CodeGeneratorBase` class for shared codegen utilities (type mapping, identifier validation, logging, etc).
- [x] Implement `InterfaceGenerator`:
    - [x] Generates an interface (e.g., `IPlayerDataInstance`) for each DataStructure.
    - [x] Only includes properties selected in `InterfacePropertyPaths` (default: value types ON, structure types OFF).
    - [x] Supports all primitive, Unity, and structure types as per requirements.
    - [x] **Interface should be named `I{DataStructureName}Instance`**
- [x] Implement `ImplementationGenerator`:
    - [x] Generates a concrete subclass (e.g., `PlayerDataInstance`) implementing the interface and inheriting from `DataInstance`.
    - [x] Maps property accessors to the runtime container using path-based access.
    - [x] **Concrete class is named `{DataStructureName}Instance` and implements the generated interface.**
- [x] Implement a `DataStructureCodeGenerator` coordinator to manage the process and file output.
    - [x] **The generated interface and implementation files are placed in the same directory as the DataStructure asset instance.**

## 3. Editor Integration
- [ ] **Do NOT add codegen UI or toggles to DataStructureEditorWindow.**
- [ ] Add the following to the DataStructureEditor (inspector) only:
    - [ ] Code generation trigger (button)
    - [ ] Interface property toggle for each path (styled, above code access info)
    - [ ] Create Instance button (enabled only if generated type is valid)
    - [ ] UI should match the style of code access boxes and appear above code snippets
- [ ] Ensure toggles update `InterfacePropertyPaths` and mark the asset dirty.

## 4. Type Resolution & Robustness
- [ ] Store only the full type name (namespace + class) in DataStructure for generated DataInstance type.
- [ ] After codegen and assembly reload, use a delayed editor call to resolve and assign the type.
- [ ] Ensure the system works regardless of assembly/project structure.
- [ ] **DataStructure is a ScriptableObject, can be created in the editor. From a DataStructure instance, generate interface and subclass. DataStructure should hold a field pointing to the generated class using full assembly qualified name.**

## 5. Testing & Validation
- [ ] Test interface and implementation generation for various DataStructures.
- [ ] Test property toggles and default states.
- [ ] Test editor integration, including codegen triggers and instance creation.
- [ ] Validate error handling and logging.

## 6. Documentation & Maintenance
- [ ] Update README and in-code XML docs.
- [ ] Document error cases and edge behaviors.
- [ ] Add usage examples and screenshots.

---

**Progress Tracking:**
- Check off each item as it is completed.
- Use this file as a living document for the reimplementation effort. 