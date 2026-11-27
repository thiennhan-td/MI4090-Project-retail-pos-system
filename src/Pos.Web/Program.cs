using Pos.Web.Demo;

var builder = WebApplication.CreateBuilder(args);

// Composition Root: đăng ký dịch vụ các phân hệ (Pos.Counter, Pos.Inventory, Pos.Infrastructure)
// sẽ được bổ sung trong các PR nghiệp vụ tiếp theo.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RetailCorePos.DemoSession";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(4);
});
builder.Services.AddSingleton<BanHangDemoSessionProvider>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
