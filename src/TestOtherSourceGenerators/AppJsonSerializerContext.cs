using System.Text.Json.Serialization;

namespace TestOtherSourceGenerators;

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
