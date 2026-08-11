# License Enforcement Specification

RefreshStatus persists Grace/Expired. Gateway TrialAccessMiddleware blocks POST/PUT/PATCH/DELETE when Expired/Revoked. Grace writes allowed with X-Trial-Status header.

