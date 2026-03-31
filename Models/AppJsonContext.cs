using System.Text.Json.Serialization;
using testASP.Models;

namespace testASP.Models;

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
internal partial class AppJsonContext : JsonSerializerContext { }
