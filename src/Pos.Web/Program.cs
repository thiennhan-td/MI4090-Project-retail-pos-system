var builder = WebApplication.CreateBuilder(args);

// Composition Root: đăng ký dịch vụ các phân hệ (Pos.Counter, Pos.Inventory, Pos.Infrastructure)
// sẽ được bổ sung trong các PR nghiệp vụ tiếp theo.
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
