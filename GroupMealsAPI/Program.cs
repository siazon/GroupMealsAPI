using GroupMealsAPI.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "allowCors",
                      policy =>
                      {
                          policy.WithOrigins("http://127.0.0.1:2712/profile");
                          policy.WithMethods("GET", "POST");
                          policy.AllowCredentials();
                      });
});
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("allowCors");
app.MapHub<ChatHub>("/Chat").RequireCors(t => t.SetIsOriginAllowed((host) => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
app.MapControllers();

app.Run();