using testASP.Models;

namespace testASP.Services.Interfaces;

public interface IAuthService
{
    AuthResponse Register(RegisterRequest request);
    AuthResponse Login(LoginRequest request);
    AuthResponse Refresh(RefreshRequest request);
    void Logout(LogoutRequest request);
    MeResponse Me(System.Security.Claims.ClaimsPrincipal user);
    void RevokeSession(System.Security.Claims.ClaimsPrincipal user, RevokeSessionRequest request);
}
