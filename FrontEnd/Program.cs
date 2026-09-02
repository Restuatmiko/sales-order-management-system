var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("CustomerService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:CustomerService"]!
    );
});

builder.Services.AddHttpClient("SalesOrderService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:SalesOrderService"]!
    );
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=SalesOrders}/{action=Index}/{id?}");

app.Run();