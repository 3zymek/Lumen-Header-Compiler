<div align="center">

<img width="700" height="218" alt="lumengine_medium2" src="https://github.com/user-attachments/assets/f0eb7356-7c7f-4293-bce2-b7c9ac7e802b" />

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
