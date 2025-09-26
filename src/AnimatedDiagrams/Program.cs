using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AnimatedDiagrams;
using AnimatedDiagrams.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<PathEditorState>();
builder.Services.AddSingleton<PensService>();
builder.Services.AddSingleton<PathEditorState>(
    sp => new PathEditorState(
        sp.GetRequiredService<PensService>(),
        sp.GetRequiredService<SettingsService>()));
builder.Services.AddSingleton<StyleRuleService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<UndoRedoService>();
builder.Services.AddSingleton<SvgFileService>();
builder.Services.AddSingleton<BrowserLocalStorage>();
builder.Services.AddSingleton<ILocalStorage, BrowserLocalStorage>();

await builder.Build().RunAsync();