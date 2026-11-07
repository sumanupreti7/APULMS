using APULMS.Models;

var builder = WebApplication.CreateBuilder(args);


// Add MVC Services
builder.Services.AddControllersWithViews();


// Register Database Helper
builder.Services.AddScoped<DatabaseHelper>();


// Enable Session
builder.Services.AddSession();


var app = builder.Build();



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}



app.UseStaticFiles();


app.UseRouting();


// Enable Session Middleware
app.UseSession();


app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);



app.Run();