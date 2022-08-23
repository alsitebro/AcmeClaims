namespace AcmeClaims

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Text
open Microsoft.AspNetCore.Http
open Twilio.Security

type TwilioSignatureValidator (options: TwilioOptions) =
    // value bindings using let represent private fields. member values are used to define public values and methods
    // 'let' and 'do' bindings must come before member and interface definitions in type definitions

    let validator = RequestValidator(options.AuthToken)
        
    let ToOrderedDictionary (form: IFormCollection): IDictionary<string, string> =
            form.OrderBy(fun k -> k.Key)
                .Select(fun k -> KeyValuePair<string,string>(k.Key, k.Value.ToString()))
                .ToDictionary ((fun x -> x.Key), (fun y -> y.Value))
    
    let BuildUrl (scheme: string, host: HostString, path: PathString, queryString: QueryString) : string = $"{scheme}://{host.Value}{path.Value}{queryString.Value}"

    let ReadBody (req: HttpRequest): string =
        if req.Body.CanSeek = false then
            req.EnableBuffering()
        req.Body.Position <- 0
        let reader = new StreamReader(req.Body, Encoding.UTF8, true, 1024)
        let body = (task { return! reader.ReadToEndAsync() }).GetAwaiter().GetResult()
        req.Body.Position <- 0
        body
        
    member _.Validate (request: HttpRequest): bool =
        if Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") = "Development" then
            true
        else
            let headers : IDictionary<string, string> = request.Headers.ToDictionary((fun h -> h.Key.ToLower()), (fun e -> e.Value.[0]))
            let signature = headers.["x-twilio-signature"]
            let contentType = headers.["content-type"]
            if contentType.Contains("application/x-www-form-urlencoded") then
                let url: string = BuildUrl (request.Scheme, request.Host, request.Path, request.QueryString)
                let parameters: IDictionary<string, string> = ToOrderedDictionary request.Form
                validator.Validate (url, parameters, signature)
            else if contentType.Contains("application/json") && request.Query.ContainsKey("bodySHA256") then
                let url: string = BuildUrl (request.Scheme, request.Host, request.Path, request.QueryString)
                let requestBody: string = ReadBody request
                validator.Validate (url, requestBody, signature)
            else
                false