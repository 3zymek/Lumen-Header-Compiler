<div align="center">

<img width="700" height="218" alt="lumengine_medium2" src="https://github.com/user-attachments/assets/f0eb7356-7c7f-4293-bce2-b7c9ac7e802b" />

# Lumen Header Compiler (LHC)

A custom code generation tool for the [LumEngine](https://github.com/3zymek/LumEngine) ecosystem.  
Parses annotated C++ headers and automatically generates repetitive boilerplate code across the engine and editor — eliminating manual maintenance overhead as the codebase grows.

---

## What it generates

| Output | Description |
|--------|-------------|
| Component Registries | Centralized component metadata, type IDs, and setup traits |
| Editor UI & Traits | ImGui inspector drawing functions and category bindings |
| Deserialization Logic | Automatic parsing functions to load components into entities |
| Pipeline Scalability | Prevents human error and boilerplate fatigue across the engine core and editor |

</div>

## How it works
```
1. Annotate your component with LHC macros
2. LHC runs before compilation via CMake
3. Generated .lum.generated.hpp files are included automatically
```


## Example

```cpp
LUM_CLASS( Category = "LIGHTNING" )
struct CPointLight : public ComponentBase {

    LUM_GENERATED_BODY( )

    LUM_PROPERTY( MinVal = 0.0, MaxVal = 1.0 )
    float32 mIntensity = 1.0f; // Light intensity in linear space

    LUM_PROPERTY( DisplayName = "Range" )
    float32 mRadius = 10.0f; // Maximum range of the light

    LUM_PROPERTY( )
    Vector3 mColor = Vector3( 1.0f ); // Light color in linear RGB

};

LUM_CLASS_EXTENSIONS( )
```

## Result
```cpp
#undef LUM_GENERATED_BODY
#define LUM_GENERATED_BODY( ) LUM_GENERATED_BODY_CPointLight( )

#undef LUM_CLASS_EXTENSIONS
#define LUM_CLASS_EXTENSIONS( ) LUM_CLASS_EXTENSIONS_CPointLight( )

//#include PLACE INCLUDES HERE IF NEEDED
namespace lum {

    #define LUM_GENERATED_BODY_CPointLight( ) \
        inline static StringView sSerializationName = "point_light"; \
        inline static StringView sDisplayName = "Point Light"; \
        inline static StringView sCategoryName = "LIGHTNING"; \
        inline static uint64 GetTypeId( ) { \
            return TypeRegistry::GetTypeId<CPointLight>( ); \
        }
    

    #define LUM_CLASS_EXTENSIONS_CPointLight( ) \
        template<> \
        struct ecs::EcsTraits<CPointLight> { \
            inline static constexpr StringView sSerializationName = "point_light"; \
            inline static constexpr StringView sDisplayName = "Point Light"; \
            inline static constexpr StringView sCategoryName = "LIGHTNING"; \
        };
    

} // namespace lum
```
<div align="center">

LHC reads this header and generates the parser, serializer and ImGui UI automatically — no manual code needed.

## Part of

Built for [LumEngine](https://github.com/3zymek/LumEngine) — a custom C++ game engine.

</div>
</div>
