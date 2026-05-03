using System.Text.Json.Serialization;

namespace Kakao.Models;

public class KakaoTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";
}

public class KakaoFriend
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = "";

    [JsonPropertyName("profile_nickname")]
    public string ProfileNickname { get; set; } = "";

    [JsonPropertyName("profile_thumbnail_image")]
    public string? ProfileThumbnailImage { get; set; }

    [JsonPropertyName("favorite")]
    public bool Favorite { get; set; }
}

public class KakaoFriendsResponse
{
    [JsonPropertyName("elements")]
    public List<KakaoFriend> Elements { get; set; } = new();

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("favorite_count")]
    public int? FavoriteCount { get; set; }

    [JsonPropertyName("before_url")]
    public string? BeforeUrl { get; set; }

    [JsonPropertyName("after_url")]
    public string? AfterUrl { get; set; }
}

public class KakaoUserProfile
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = "";

    [JsonPropertyName("thumbnail_image_url")]
    public string? ThumbnailImageUrl { get; set; }
}

public class KakaoAccount
{
    [JsonPropertyName("profile")]
    public KakaoUserProfile? Profile { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class KakaoUserInfo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("kakao_account")]
    public KakaoAccount? KakaoAccount { get; set; }
}

public class KakaoApiException : Exception
{
    public System.Net.HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public KakaoApiException(System.Net.HttpStatusCode statusCode, string responseBody)
        : base($"Kakao API {(int)statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
