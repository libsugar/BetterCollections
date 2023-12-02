# BetterCollections

[![.NET](https://github.com/libsugar/BetterCollections/actions/workflows/dotnet.yml/badge.svg)](https://github.com/libsugar/BetterCollections/actions/workflows/dotnet.yml)
[![Nuget](https://img.shields.io/nuget/v/BetterCollections)](https://www.nuget.org/packages/BetterCollections/)
![MIT](https://img.shields.io/github/license/libsugar/BetterCollections)

### WIP

- All containers optionally use ArrayPool
- Buffers
  - `DirectAllocationArrayPool<T>`  
    Pretend to have pooling
  - `ArrayPoolFactory`  
    Factory abstraction provides array pool implementation
- Sync
  - `Vec<T>`  
    Reimplemented `List<T>`, can unsafely retrieve the internal Array or as Span, Memory
    *todo* methods same to `List<T>`
- Concurrent
  - `OnceInit<T>`  
    Similar to `Lazy<T>`, but provides init function when getting  
    Inspired by rust `OnceCell<T>`
- Memories
  - `Box<T>` `ReadOnlyBox<T>`  
    Simply wrap a value onto the heap
  - `ArrayRef<T>` `ReadOnlyArrayRef<T>`  
    A **fat reference** *(`T[] + offset`)* that can be safely put on the **heap**, not a ref struct
  - `MemoryRef<T>` `ReadOnlyMemoryRef<T>`  
    A **fat reference** *(`Memory<T> + offset`)* that can be safely put on the **heap**, not a ref struct
  - `OffsetRef<T>` `ReadOnlyOffsetRef<T>`  
    A **fat reference** *(`object + offset`)* that can be safely put on the **heap**, not a ref struct
  - `MemoryEx`
    - Provides `GetEnumerator` for `Memory<T>` `ReadOnlyMemory<T>`
