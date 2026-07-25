using System.Collections.ObjectModel;
using System.Numerics;
using System.Windows.Media.Media3D;
using ModelAssets;
using ModelAssets.Wpf;

var positions = Array.AsReadOnly(new[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY });
var primitive = new RuntimePrimitive(
    new RuntimeVertexBuffer(positions, Array.Empty<Vector3>(), Array.Empty<Vector2>()),
    new RuntimeIndexBuffer(Array.AsReadOnly(new uint[] { 0, 1, 2 })),
    0,
    new RuntimeBounds(Vector3.Zero, Vector3.One));
var model = RuntimeModelAsset.Freeze(
    new string('a', 64),
    [new RuntimeNode(0, "Piece", 0, Array.Empty<int>(), RuntimeTransform.Identity, RuntimeTransform.Identity)],
    [new RuntimeMesh("Triangle", new ReadOnlyCollection<RuntimePrimitive>([primitive]))],
    [new RuntimeMaterial("Ivory", new(0.9f, 0.85f, 0.7f, 1), null, RuntimeAlphaMode.Opaque, 0.5f, true)],
    [],
    new RuntimeBounds(Vector3.Zero, Vector3.One),
    new RuntimeModelDiagnostics(TimeSpan.Zero, 64, [], []));

var factory = new WpfRuntimeModelFactory();
var first = factory.Create(model);
var second = factory.Create(model);
if (!first.Model.IsFrozen || !ReferenceEquals(first.Model, second.Model) || !second.FromCache)
    throw new InvalidOperationException("WPF model cache/freeze contract failed.");
var node = (Model3DGroup)first.Model.Children[0];
var geometry = (GeometryModel3D)node.Children[0];
if (!geometry.Geometry.IsFrozen || geometry.BackMaterial is null)
    throw new InvalidOperationException("WPF geometry/double-sided material contract failed.");

var glb = ModelFormatResolver.Resolve(
    [new("glb", "piece.glb", true, true), new("obj", "piece.obj", true, true)]);
if (glb.Selection != ModelFormatSelection.Glb) throw new InvalidOperationException("GLB was not preferred.");
var obj = ModelFormatResolver.Resolve(
    [new("glb", "piece.glb", true, false, "unsupported skin"), new("obj", "piece.obj", true, true)]);
if (obj.Selection != ModelFormatSelection.Obj || obj.Reason != ModelFallbackReason.ObjFallback)
    throw new InvalidOperationException("OBJ fallback was not selected.");
var procedural = ModelFormatResolver.Resolve(
    [new("glb", "piece.glb", false, false), new("obj", "piece.obj", false, false)]);
if (procedural.Selection != ModelFormatSelection.Procedural ||
    procedural.Reason != ModelFallbackReason.ProceduralFallback)
    throw new InvalidOperationException("Procedural fallback was not selected.");

Console.WriteLine("Model asset WPF contracts passed.");
return 0;
