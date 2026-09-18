using CampGearApp;
using CampGearApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text;

// Shift-JIS(日本語Windows版Excelの標準的なCSV文字コード)を読み込めるようにする
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// appsettings.jsonから、Google Apps ScriptのURLを読み込む
var appsScriptUrl = builder.Configuration["GoogleAppsScriptUrl"]
    ?? throw new InvalidOperationException("GoogleAppsScriptUrlがappsettings.jsonに設定されていません。");

// HttpClientを1つ作成し、GearDataServiceに渡す
var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
builder.Services.AddSingleton(httpClient);

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<HttpClient>();
    return new GearDataService(client, appsScriptUrl);
});

var host = builder.Build();

// アプリ起動時に、サーバー上のデータを読み込んでおく
var dataService = host.Services.GetRequiredService<GearDataService>();
await dataService.LoadFromServerAsync();

await host.RunAsync();