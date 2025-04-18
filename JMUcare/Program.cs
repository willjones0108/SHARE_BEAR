
//builder.Services.AddScoped<BlobStorageService>(); // Blob storage service for document handling


using JMUcare.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSession(); // Enables session state
builder.Services.AddHttpContextAccessor(); // Provides access to the current HTTP context

// Register custom services for local storage
builder.Services.AddScoped<BlobStorageService>(); // Local storage service for document handling

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); // Use a custom error page in production
    app.UseHsts(); // Enforce HTTPS in production
}

app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS
app.UseStaticFiles(); // Serve static files like CSS, JS, and uploaded files

app.UseRouting(); // Enable routing

app.UseSession(); // Enable session middleware
app.UseAuthorization(); // Enable authorization middleware

app.MapRazorPages(); // Map Razor Pages endpoints

app.Run(); // Run the application