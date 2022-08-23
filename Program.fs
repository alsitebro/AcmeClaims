namespace AcmeClaims

open MongoDB.Driver

#nowarn "20"
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging.ApplicationInsights

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()
        builder.Services.AddEndpointsApiExplorer()
        builder.Services.AddSwaggerGen()
        builder.Services.AddApplicationInsightsTelemetry()
        builder.Services.AddMemoryCache()

        let twilioOptions = new TwilioOptions()
        builder.Configuration.GetSection("Twilio").Bind(twilioOptions)
        builder.Services.AddScoped<TwilioOptions>(fun sp -> twilioOptions)
        builder.Services.AddScoped<TwilioSignatureValidator>()
        let mongoDbOptions = new MongoDBOptions()
        builder.Configuration.GetSection("MongoDB").Bind(mongoDbOptions)
        builder.Services.AddScoped<IMongoDatabase>(fun sp -> MongoClient(connectionString = mongoDbOptions.ConnectionString).GetDatabase("acme_claims"))

        let app = builder.Build()
        
        if app.Environment.IsDevelopment() = true then 
            app.UseSwagger().UseSwaggerUI() |> ignore


        app.Use(Middleware.requestLogger)
        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Use(Middleware.responseLogger)
        app.Run()

        exitCode