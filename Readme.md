# Programmable Voice Demo
This project is a sample ASP.NET Core minimal API application

It demonstrates the implementaton of a simple Intelligent Voice Routing (IVR) service which handles requests from the Twilio Programmable Voice platform by returning responses containing TwiML instructions.

## Routes
> POST claims/start

> POST claims/gather/option

> POST claims/gather/account

> POST claims/fail

> POST claims/redirect/underwriter

> POST claims/complete

> POST identity/gather/account

> POST identity/gather/postcode

> POST identity/fail

> POST identity/verify