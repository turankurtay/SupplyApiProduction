using Nethereum.Web3;
using SupplyApi.Model;
using SupplyApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.Configure<WalletSettings>(builder.Configuration.GetSection("WalletSettings"));
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IEthBalanceService, EthBalanceService>();
builder.Services.AddHttpClient<ISolBalanceService, SolBalanceService>();
builder.Services.AddHttpClient<IBscBalanceService, BscBalanceService>();

builder.Services.AddSingleton(new Web3("https://bsc-dataseed.binance.org/"));
builder.Services.AddSingleton(provider => new Web3("https://api.avax.network/ext/bc/C/rpc"));
builder.Services.AddSingleton<IEthBalanceService, EthBalanceService>();
builder.Services.AddSingleton<ISolBalanceService, SolBalanceService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run("https://0.0.0.0:5000");
//app.Run();

