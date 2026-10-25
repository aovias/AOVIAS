# Phase V9 — Checkout Experience Report

## Routes

`/checkout` → `/checkout/billing` → `/checkout/confirm` → `/checkout/success`

## Design intent

Enterprise procurement review — **not** consumer e-commerce.

## Shows

- Selected plan  
- Commercial summary  
- Organization / contact details  
- Next steps  

## Enterprise gate

Plans with `requiresSales` block self-serve billing and route to Enterprise Sales.

## Messaging

- Cart: “Review your commercial selection”  
- Billing: “Contact and organization information”  
- Confirm: “Review commercial summary”  
- Success: “Commercial request recorded” — most buyers continue via sales  

Payment is not processed on the public site until Commerce infrastructure is live.
