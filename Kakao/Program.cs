using System.Text.Json;
using Kakao.Models;
using Kakao.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddScoped<KakaoAuthService>();
builder.Services.AddScoped<KakaoFriendsService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

// 1. 카카오 로그인 페이지로 리다이렉트
app.MapGet("/login", (KakaoAuthService auth) =>
    Results.Redirect(auth.GetAuthorizationUrl()));

// 2. OAuth 콜백: 인가 코드 -> 액세스 토큰 교환 후 쿠키에 저장
app.MapGet("/oauth/callback", async (string code, KakaoAuthService auth, HttpContext ctx) =>
{
    var token = await auth.ExchangeCodeForTokenAsync(code);
    ctx.Response.Cookies.Append("kakao_token", token.AccessToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        MaxAge = TimeSpan.FromHours(6)
    });
    return Results.Redirect("/");
});

// 3. 로그인 유저 정보 API
app.MapGet("/api/me", async (KakaoAuthService auth, HttpContext ctx) =>
{
    var accessToken = ctx.Request.Cookies["kakao_token"];
    if (string.IsNullOrEmpty(accessToken))
        return Results.Unauthorized();

    try
    {
        var user = await auth.GetUserInfoAsync(accessToken);
        return Results.Ok(new
        {
            id = user.Id,
            nickname = user.KakaoAccount?.Profile?.Nickname,
            thumbnailImageUrl = user.KakaoAccount?.Profile?.ThumbnailImageUrl,
            email = user.KakaoAccount?.Email
        });
    }
    catch (KakaoApiException ex)
    {
        return Results.Json(new { kakaoError = ex.ResponseBody }, statusCode: 502);
    }
});

// 4. 친구 목록 API
app.MapGet("/api/friends", async (KakaoFriendsService friendsService, HttpContext ctx) =>
{
    var accessToken = ctx.Request.Cookies["kakao_token"];
    if (string.IsNullOrEmpty(accessToken))
        return Results.Unauthorized();

    try
    {
        var friends = await friendsService.GetAllFriendsAsync(accessToken);
        return Results.Ok(friends);
    }
    catch (KakaoApiException ex)
    {
        if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            ctx.Response.Cookies.Delete("kakao_token");

        try
        {
            var kakaoErr = JsonSerializer.Deserialize<JsonElement>(ex.ResponseBody);
            if (kakaoErr.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() == -501)
                return Results.Json(
                    new { error = "이 카카오 계정은 카카오톡 사용자가 아닙니다. 카카오톡이 설치된 계정으로 로그인하거나, 개발자 콘솔에서 테스터로 등록하세요.", code = -501 },
                    statusCode: 400);
        }
        catch { }

        // 502로 반환해서 프론트가 "로그인 필요 401"과 구분하게 함
        return Results.Json(new { kakaoError = ex.ResponseBody }, statusCode: 502);
    }
});

// 5. 설정값 확인 (디버그용 - 배포 확인 후 삭제)
app.MapGet("/debug/config", (IConfiguration config) =>
{
    var restApiKey = config["Kakao:RestApiKey"];
    var redirectUri = config["Kakao:RedirectUri"];
    return Results.Ok(new
    {
        restApiKey_length = restApiKey?.Length ?? 0,
        restApiKey_preview = restApiKey?.Length > 4 ? restApiKey[..4] + "***" : "(비어있음)",
        redirectUri = redirectUri ?? "(비어있음)",
        aspnetcore_env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "(없음)"
    });
});

// 5. 토큰 스코프 확인 (디버그용)
app.MapGet("/debug/token", async (IHttpClientFactory factory, HttpContext ctx) =>
{
    var accessToken = ctx.Request.Cookies["kakao_token"];
    if (string.IsNullOrEmpty(accessToken))
        return Results.Json(new { error = "쿠키에 토큰 없음. 먼저 /login 으로 로그인하세요." }, statusCode: 401);

    var client = factory.CreateClient();
    var req = new HttpRequestMessage(HttpMethod.Get, "https://kapi.kakao.com/v1/user/access_token_info");
    req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    var response = await client.SendAsync(req);
    var body = await response.Content.ReadAsStringAsync();
    return Results.Content(body, "application/json");
});

// 6. 로그아웃 - 카카오 서버 로그아웃 후 콜백으로 돌아옴
app.MapGet("/logout", (KakaoAuthService auth) =>
    Results.Redirect(auth.GetLogoutUrl()));

// 7. 카카오 로그아웃 콜백 - 로컬 쿠키 삭제
app.MapGet("/oauth/logout-callback", (HttpContext ctx) =>
{
    ctx.Response.Cookies.Delete("kakao_token");
    return Results.Redirect("/");
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
