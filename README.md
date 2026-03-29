<div align="center">

![logo](https://raw.githubusercontent.com/3zymek/LumEngine/main/LumEngine/internal_assets/branding/lumengine_medium2.png)

# Lumen Header Compiler (LHC)

A code generation tool for the [LumEngine](https://github.com/3zymek/LumEngine) ecosystem.  
Parses annotated C++ headers and generates boilerplate — no more manual parsers, serializers or editor UI.

---

## What it generates

| Output | Description |
|--------|-------------|
| Scene parsers | Reads `.lsc` scene files into components |
| Serializers | Writes component state to `.lsc` |
| ImGui UI | Editor property panels per component |
| Dirty setters | Automatic `bDirty` flagging on property change |

</div>

## How it works
```
1. Annotate your component with LHC macros
2. LHC runs before compilation via CMake
3. Generated .lum.generated.hpp files are included automatically
```

## Example
```cpp
LCLASS()
struct CTransform : public Component {
    LPROPERTY(Edit) glm::vec3 mPosition;
    LPROPERTY(Edit) glm::vec3 mRotation;
    LPROPERTY(Edit) glm::vec3 mScale;
};
```

LHC reads this header and generates the parser, serializer and ImGui UI automatically — no manual code needed.

## Part of

<div align="center">

Built for [LumEngine](https://github.com/3zymek/LumEngine) — a custom C++ game engine.

</div>
