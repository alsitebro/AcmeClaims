namespace AcmeClaims

open System.Collections.Generic
open System.Linq
open Microsoft.AspNetCore.Http

module Helpers =
    let ToDictionary (form: IFormCollection): IDictionary<string, string> =
            form.Select(fun k -> KeyValuePair<string,string>(k.Key, k.Value.ToString()))
                .ToDictionary ((fun x -> x.Key), (fun y -> y.Value))