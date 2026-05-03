using System.Text.Json;
using Kakao.Models;

namespace Kakao.Services;

public class KakaoAuthService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public KakaoAuthService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public string GetAuthorizationUrl()
    {
        var clientId = _config["Kakao:RestApiKey"];
        var redirectUri = _config["Kakao:RedirectUri"];
        return $"https://kauth.kakao.com/oauth/authorize" +
               $"?client_id={clientId}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}" +
               $"&response_type=code" +
               $"&scope=friends" +
               $"&prompt=consent";
    }

    public async Task<KakaoUserInfo> GetUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "https://kapi.kakao.com/v2/user/me");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await client.SendAsync(req);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new KakaoApiException(response.StatusCode, errorBody);
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KakaoUserInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new Exception("유저 정보 파싱 실패");
    }

    public string GetLogoutUrl()
    {
        var clientId = _config["Kakao:RestApiKey"];
        var logoutRedirectUri = _config["Kakao:LogoutRedirectUri"];
        return $"https://kauth.kakao.com/oauth/logout" +
               $"?client_id={clientId}" +
               $"&logout_redirect_uri={Uri.EscapeDataString(logoutRedirectUri!)}";
    }

    public async Task<KakaoTokenResponse> ExchangeCodeForTokenAsync(string code)
    {
        var clientId = _config["Kakao:RestApiKey"]!;
        var redirectUri = _config["Kakao:RedirectUri"]!;
        var clientSecret = _config["Kakao:ClientSecret"] ?? "";

        var client = _httpClientFactory.CreateClient();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["code"] = code,
        };

        if (!string.IsNullOrEmpty(clientSecret))
            formData["client_secret"] = clientSecret;

        var response = await client.PostAsync(
            "https://kauth.kakao.com/oauth/token",
            new FormUrlEncodedContent(formData)
        );

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<KakaoTokenResponse>(json)
               ?? throw new Exception("토큰 응답 파싱 실패");
    }
}
