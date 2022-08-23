namespace AcmeClaims
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

        let app = builder.Build()
        
        //if app.Environment.IsDevelopment() = true then 
        //    app.UseSwagger().UseSwaggerUI() |> ignore

        app.UseSwagger().UseSwaggerUI() |> ignore


        app.Use(Middleware.requestLogger)
        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()

        app.Use(Middleware.responseLogger)
        app.Run()

        exitCode