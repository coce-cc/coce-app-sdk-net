using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CoceAppSdk;

public static class CoceAppSdkExtensions
{
    public static IServiceCollection AddCoceAppSdk(this IServiceCollection builder,
        Action<CoceAppSdkOption>? options = null)
    {
        var coceSdkOption = new CoceAppSdkOption();
        options?.Invoke(coceSdkOption);
        builder.AddSingleton(coceSdkOption);
        builder.AddSingleton<CoceApp>();
        return builder;
    }

    public static IApplicationBuilder UseCoceAppSdk(this IApplicationBuilder app)
    {
        var option = app.ApplicationServices.GetRequiredService<CoceAppSdkOption>();
        return app;
    }
}