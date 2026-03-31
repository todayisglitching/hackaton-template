using System.Text.Json.Serialization;
using testASP.Models;
using testASP.NoSqlDb.Models;
using testASP.Services;

namespace testASP.Models;

[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(List<FieldDefinition>))]
[JsonSerializable(typeof(List<DynamicField>))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(LogoutRequest))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(MeResponse))]
[JsonSerializable(typeof(DeviceDto))]
[JsonSerializable(typeof(DeviceCreateRequest))]
[JsonSerializable(typeof(DeviceSelectRequest))]
[JsonSerializable(typeof(DeviceListResponse))]
[JsonSerializable(typeof(FieldDefinition))]
[JsonSerializable(typeof(DynamicField))]
internal partial class AppJsonContext : JsonSerializerContext { }
