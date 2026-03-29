# Lumen Header Compiler (LHC)

LHC is a code generation tool built for the LumEngine ecosystem.
It parses C++ header files annotated with reflection macros and automatically 
generates boilerplate code — eliminating the need to manually write 
parsers, serializers, and editor UI for every new component.

## What it generates
- Scene file parsers (.lsc format)
- Scene serializers
- ImGui editor UI
- Dirty flag setters

## How it works
1. Annotate your component with LHC macros
2. LHC runs before compilation via CMake
3. Generated .lum.generated.hpp files are included automatically

## Example
```cpp
LCLASS()
struct CTransform {
    LPROPERTY(Edit) glm::vec3 mPosition;
    LPROPERTY(Edit) glm::vec3 mRotation;
    LPROPERTY(Edit) glm::vec3 mScale;
};
```

LHC generates the parser, serializer and ImGui UI for this component automatically.

## Part of
<div align="center">
![lumengine logo](https://github.com/3zymek/LumEngine/blob/main/LumEngine/internal_assets/branding/lumengine_medium2.png)
[LumEngine](https://github.com/3zymek/LumEngine) — a custom C++ game engine.
