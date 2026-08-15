# Cubemap conversion pipeline

## Threading contract

`MaterialEditorCubemapCache.TryAcquire` is the synchronous compatibility path.
It performs source decoding, projection, upload, and cache publication on the
calling thread and must only be used from Unity's main thread. PNG decoding uses
Unity APIs; Radiance HDR decoding is managed but the resulting Cubemap upload
still uses Unity APIs.

Interactive imports should use `MaterialEditorCubemapCache.TryBeginAcquire`.
After a cache miss, a coroutine calls `MaterialEditorCubemapAcquireOperation.ProcessRows`
with a bounded row budget and yields between calls. This distributes panorama
sampling across frames. It does not move Unity APIs to a worker thread.

Reading the source file and computing its SHA-256 content key can happen on a
worker thread. Use `MaterialEditorCubemapContentKey.TryCompute`, then pass the
returned opaque key and the same byte-array instance to the keyed
`TryBeginAcquire` overload; this avoids repeating the hash on the main thread.
The coordinator keeps its warm cache lease alive while the repository passes
that key to the character or Studio controller. The controller performs a keyed
cache hit and stores a separate lease of its own before the coordinator disposes
the warm lease; lease ownership is never transferred or shared implicitly.
`Texture2D.LoadImage`, `GetPixels32`, `Cubemap`, `SetPixels`, `Apply`, cache lease
publication, and lease disposal remain main-thread operations. PNG decode and
`GetPixels32` are therefore still one indivisible main-thread phase that must be
profiled on every supported Unity 5.6 target. Radiance RGBE `.hdr` sources use a
managed incremental scanline decoder and incremental face projection, then upload
to an `RGBAHalf` Cubemap on the main thread. The original HDR bytes remain the
persisted/cache source; no schema or public API format tag is required.

## Cache publication

Cache lookup and reference counts are protected by a short global lock. A cache
miss converts outside that lock. Publication performs a second lookup and either
publishes the candidate or acquires the winner; a losing Unity Cubemap is destroyed
after leaving the lock on the main thread.

## Memory guardrail

`MaterialEditorCubemapMemoryBudget` calculates conservative import and export peak
estimates and rejects operations above the current 320 MiB budget. The estimate
accounts for managed buffers and expected CPU/GPU staging copies, but it cannot see
native decoder or driver allocations. Runtime Unity profiler evidence is still
required before changing source limits or the budget.

Imports and exports reserve their estimates from one shared admission budget.
An incremental import keeps that reservation between frames, so a concurrent
import or export is rejected when the combined estimates would exceed 320 MiB.
Reservations are released on completion, failure, or cancellation.

## GPU export validation gate

The non-readable fallback reuses one Camera, Skybox, material, RenderTexture, and
readable Texture2D for all six faces. It intentionally retains
`RenderTextureReadWrite.Default` until CPU-readable export, GPU-readback export,
and export/re-import are compared with a direction-labelled, color-coded Cubemap
on each supported Maker and Studio target. Selecting Linear or sRGB without that
evidence could silently change exported values.

## Original-value identity

Inherited original snapshots match material bindings by component kind, root-relative
name path, component index, material slot, formatted material name, and property.
Absolute sibling indexes are deliberately excluded, so moving or inserting an
unrelated sibling does not change identity. Mappings must still be exact and
unique: duplicate name paths are rejected instead of guessed. Count-only or
traversal-order remapping is not allowed; ambiguous mappings fail safely and
capture the destination originals.
