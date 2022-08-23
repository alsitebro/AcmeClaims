# Programmable Voice Demo
This project is a ASP.NET Core API application written in F#

It demonstrates the implementaton of a simple Intelligent Voice Routing (IVR) service which handles requests from the Twilio Programmable Voice platform by returning responses containing TwiML instructions.

This application was implemented using the Twilio SDK for .NET

This demo was used in my presentation at Peterborough .NET 23rd August 2022

## Routes
> POST claims/start

> POST claims/options

> POST claims/fail

> POST claims/underwriter

> POST claims/complete

> POST identity/account

> POST identity/fail

> POST identity/verify

##  Resources
> [Twilio Programmable Voice Documentation](https://www.twilio.com/docs/voice)

> [Programmable Voice Quickstart for C# / .NET](https://www.twilio.com/docs/voice/quickstart/csharp)

> [TwiML™ for Programmable Voice](https://www.twilio.com/docs/voice/twiml)

> [Twilio signature validation for webhook security] (https://www.twilio.com/docs/usage/webhooks/webhooks-security#validating-signatures-from-twilio)

> [Twilio SDK for .NET](https://www.nuget.org/packages/Twilio)

> [ASP.NET Core Libraries for the Twilio SDK](https://www.nuget.org/packages/Twilio.AspNet.Core)

> Git repositories

[Twilio ASP.NET Core](https://github.com/twilio-labs/twilio-aspnet)

[Twilio SDK](https://github.com/twilio/twilio-csharp)

> Install packages via dotnet CLI

dotnet add package Twilio --version 5.78.1

dotnet add package Twilio.AspNet.Core --version 6.0.0