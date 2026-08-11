# CRP Phase 1 — Deployment Checklist

- [ ] Deploy Shared.Infrastructure, Shared.Logging, IdentityService, CommunicationService, Gateway, WebApp  
- [ ] Set Production `JwtSettings:SecretKey` from secret store  
- [ ] Set `Smtp:*` and `Email:Provider=Smtp` for Identity + Communication  
- [ ] Set `Identity:PublicBaseUrl` to public SPA origin  
- [ ] Confirm `RelaxLoginSecurityGates=false`  
- [ ] Confirm `EmailVerificationMode` intended value on Identity **and** Gateway  
- [ ] Restart Identity/Gateway/Communication (avoid DLL file locks)  
- [ ] Smoke: `/register` → verify email → login  
- [ ] Smoke: `/signup` → onboarding wizard still works (Async mode)  
- [ ] Smoke: forgot/reset password email  
- [ ] Confirm rate limit returns 429 under abuse  
