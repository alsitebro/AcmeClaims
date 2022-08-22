namespace AcmeClaims

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

module Middleware =
    let requestLogger (ctx: HttpContext) (nxt: RequestDelegate): Task =
        Console.WriteLine($"REQUEST: {ctx.Request.Method} {ctx.Request.Path}")
        nxt.Invoke(ctx)

    let responseLogger (ctx: HttpContext) (nxt: RequestDelegate): Task =
        Console.WriteLine($"RESPONSE: {ctx.Response.StatusCode} {ctx.Response.ContentType}")
        nxt.Invoke(ctx)