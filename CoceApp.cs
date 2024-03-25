using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SimApi.Communications;
using SimApi.Helpers;

namespace CoceAppSdk;

public class CoceApp(CoceAppSdkOption option, ILogger<CoceApp> logger)
{
    /// <summary>
    /// 获取Level Token
    /// </summary>
    /// <param name="lv1Token"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public LevelTokenResponse? GetLevelToken(string lv1Token, int level = 5)
    {
        var platUrl = option.ApiEndpoint + "/api/app/token";
        var dict = new Dictionary<string, object>
        {
            { "lv1Token", lv1Token },
            { "time", (int)SimApiUtil.TimestampNow },
            { "appId", option.AppId! },
            { "level", level }
        };
        var sorted = dict.OrderBy(x => x.Key);
        var signStr = sorted.Aggregate("", (current, item) => current + $"{item.Key}={item.Value}&").TrimEnd('&');
        logger.LogDebug("签名的字符串: {SignStr}", signStr);
        var sign = SimApiUtil.Md5(signStr + option.AppKey);
        logger.LogDebug("签名: {Sign}", sign);
        var http = new HttpClient();
        dict.Add("sign", sign);
        logger.LogDebug("请求地址: {PlatUrl} => {Data}", platUrl, JsonSerializer.Serialize(dict));
        var response = http.PostAsJsonAsync(platUrl, dict).Result;
        var result = response.Content.ReadFromJsonAsync<SimApiBaseResponse<LevelTokenResponse>>().Result!;
        if (result.Code != 200)
        {
            logger.LogDebug("发生错误: {Code} => {Message}", result.Code, result.Message);
            return null;
        }
        return result.Data;
    }

    /// <summary>
    /// 获取用户的群组信息
    /// </summary>
    /// <param name="token">Level >=2 的Token</param>
    /// <returns></returns>
    public IEnumerable<GroupInfo>? GetUserGroups(string token)
    {
        const string uri = "/api/lv2/user/groups";
        var resp = ProxyQuery<GroupInfo[]>(uri, token, "{}");
        return resp;
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public UserInfo? GetUserInfo(string token)
    {
        const string uri = "/api/lv1/user/info";
        return ProxyQuery<UserInfo>(uri, token);
    }


    public dynamic? ProxyQuery(string uri, string token, string json) => ProxyQuery<dynamic>(uri, token, json);

    public dynamic? ProxyQueue(string uri, string token, object data) =>
        ProxyQuery<dynamic>(uri, token, JsonSerializer.Serialize(data));

    public T? ProxyQueue<T>(string uri, string token, object data) =>
        ProxyQuery<T>(uri, token, JsonSerializer.Serialize(data));

    public T? ProxyQuery<T>(string uri, string token, string json = "{}")
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Token", token);
        var realUrl = option.ApiEndpoint + uri;
        var response = http.PostAsync(realUrl, new StringContent(json, Encoding.UTF8, "application/json"))
            .Result;
        var resp = response.Content.ReadFromJsonAsync<SimApiBaseResponse<T>>().Result!;
        if (resp.Code != 200)
        {
            logger.LogDebug("请求发生错误: {RespCode} => {RespMessage}", resp.Code, resp.Message);
        }
        return resp.Code == 200 ? resp.Data : default;
    }

    public ConfigResponse GetConfig()
    {
        return new ConfigResponse(option.AppId!, option.AuthEndpoint);
    }
}

public record ConfigResponse(string AppId, string AuthUrl);

public record LevelTokenResponse(string Token, string UserId, int TokenLevel);

public record GroupInfo(string Id, string Name, string Image, string Description, string Role);

public record UserInfo(string UserId, string Name, string Image);