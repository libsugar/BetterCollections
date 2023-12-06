# BetterCollections

[![.NET](https://github.com/libsugar/BetterCollections/actions/workflows/dotnet.yml/badge.svg)](https://github.com/libsugar/BetterCollections/actions/workflows/dotnet.yml)
[![Nuget](https://img.shields.io/nuget/v/BetterCollections)](https://www.nuget.org/packages/BetterCollections/)
![MIT](https://img.shields.io/github/license/libsugar/BetterCollections)
[![ApiDoc](https://img.shields.io/badge/ApiDoc-222222?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABoAAAAXCAYAAAAV1F8QAAAACXBIWXMAAAsSAAALEgHS3X78AAABpElEQVRIib2WQU7CUBCGfwh7dOdO8sc9uHMnJzC6N7Hu3KknsJzA3kC8AZ5AuEE5ABNMXLhE414zZJ4OpS1gin8C75U3ne9N5zHT2mQyiQC0sKghgC7KNQMwIDldYTdXwxxe5KzdrXH/vYiMAMQkh2WGdZIaUW+dXRXoGMCziCS6LCI7uSD9IhkDeCpw9A7gkGQtfADsAjgDMHJ21yLSBxCJyGkuyJQWgFKSC2skZyQ1P/rYL92SpmAPQF9Eok1BpSLZz8CuFATgwSJcAs3+AsIvLDz6JoCPEGGIrF58+8aK3Q0nAMY2jysFWR5f7LLtUrEvIp0qI1IV/XkrB3m9uXnrv0CVHoZSbRN04C+2Cfp082nVoI6NesyPbK61clAZyCpA0y4HVtVVidbGSkDWGhL306uNY+sMCznqIF+5/SUDGbpotLed2yP7aRceVOSwXQLpGiTYPFpx1leDrm/zDberqMRhag5S25A60tFvQiFaxW90XfPifTSsG6pBCL0sqpDgrG5JJiKigKXuqtK3oC87jr4gzltyxtZDgr2ertVvQgC+AfGSgtzXl3gYAAAAAElFTkSuQmCC)](https://libsugar.github.io/BetterCollections/)

### WIP

- All containers optionally use ArrayPool
- Buffers
  - `DirectAllocationArrayPool<T>`  
    Pretend to have pooling
  - `ArrayPoolFactory`  
    Factory abstraction provides array pool implementation
- Not Sync / Non Concurrent / No Thread Safe
  - `Vec<T>`  
    Reimplemented `List<T>`, can unsafely retrieve the internal Array or as Span, Memory
    *todo* methods same to `List<T>`
  - `FlatHashMap<K, V>`  
    HashMap with simd accelerated query, using [SwissTables](https://abseil.io/blog/20180927-swisstables) algorithm  
    simd support see [Simd Support](#Simd-Support)
- Sync / Concurrent / Thread safe
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
- Cryptography
  - `AHasher`  
    A hasher that ensures even distribution of each bit  
    If possible use Aes SIMD acceleration (.net7+)

#### Simd Support
  - .net8 +
    - Vector512
  - .net7 +
    - Vector256
    - Vector128
    - Vector64
    - X86.Aes
    - Arm.Aes
  - other
    - soft  

.Net 6 does not fully support simd, so .net6 does not have simd support  
**Don’t use .net6, use .net8+**
