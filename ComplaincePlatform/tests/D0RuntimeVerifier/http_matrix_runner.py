import json
import ssl
import time
import uuid
import urllib.error
import urllib.request


GW = "http://localhost:5212"
TOKEN_A = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI0MGJmZGU4Yi1mYjIyLTRhYTctOThhYS1lNjY3YmIzODZjM2MiLCJlbWFpbCI6ImQwaHR0cGFAZXhhbXBsZS5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjQwYmZkZThiLWZiMjItNGFhNy05OGFhLWU2NjdiYjM4NmMzYyIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6ImQwaHR0cGFAZXhhbXBsZS5jb20iLCJqdGkiOiJmZmQzMzM4ZS05NDcxLTQ1YzUtODJiNi1jNzdlNDBhMzllMDQiLCJvcmdhbml6YXRpb25faWQiOiIxMTExMTExMS0xMTExLTExMTEtMTExMS0xMTExMTExMTExMTEiLCJob3NwaXRhbF9pZCI6ImFhYWFhYWFhLWFhYWEtYWFhYS1hYWFhLWFhYWFhYWFhYWFhYSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6Ik9SR19BRE1JTiIsInNjb3BlIjoiY29tcGxpYW5jZS5hcGkgb3JnYW5pemF0aW9uLmFwaSBhaS5hcGkiLCJleHAiOjE3NzcwMjI4NjUsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjcxMzUiLCJhdWQiOiJodHRwczovL2xvY2FsaG9zdDo3MTM1In0.aeNf0_glO43FrbXoVETlasRoH0YKAcjZK9bm6bsF8tY"
TOKEN_B = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJiYmNiZmRkNC0zNTA2LTRmMTktOGZiYS00YTBiZjQyNjgxMDgiLCJlbWFpbCI6ImQwaHR0cGJAZXhhbXBsZS5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImJiY2JmZGQ0LTM1MDYtNGYxOS04ZmJhLTRhMGJmNDI2ODEwOCIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6ImQwaHR0cGJAZXhhbXBsZS5jb20iLCJqdGkiOiJkOTk1NzNlZi0yNDI4LTRlMDEtYjA2Yi02ZjAwMTdlYTBjZWIiLCJvcmdhbml6YXRpb25faWQiOiIyMjIyMjIyMi0yMjIyLTIyMjItMjIyMi0yMjIyMjIyMjIyMjIiLCJob3NwaXRhbF9pZCI6ImJiYmJiYmJiLWJiYmItYmJiYi1iYmJiLWJiYmJiYmJiYmJiYiIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6Ik9SR19BRE1JTiIsInNjb3BlIjoiY29tcGxpYW5jZS5hcGkgb3JnYW5pemF0aW9uLmFwaSBhaS5hcGkiLCJleHAiOjE3NzcwMjI4NjUsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjcxMzUiLCJhdWQiOiJodHRwczovL2xvY2FsaG9zdDo3MTM1In0.fqBj7HEDOnbmbpmQ7z6xU7nfkugCsTCG47ZhzvEMaGA"

CORS = str(uuid.uuid4())

START_BODY = {
    "goal": "AuditPreparationWorkflow",
    "aiRequest": {
        "prompt": "Prepare audit plan",
        "useCase": 5,
        "hospitalId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        "contextVersionId": "ctx-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-v1",
        "promptTemplateId": "TaskRecommendation",
        "promptTemplateVersion": "1.0",
    },
}

ASSIST_BODY = {
    "prompt": "Give me recommendation",
    "useCase": 5,
    "hospitalId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "contextVersionId": "ctx-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-v1",
    "promptTemplateId": "TaskRecommendation",
    "promptTemplateVersion": "1.0",
}


def call(method: str, path: str, token: str, body=None):
    data = None if body is None else json.dumps(body).encode("utf-8")
    req = urllib.request.Request(f"{GW}{path}", method=method, data=data)
    req.add_header("Authorization", f"Bearer {token}")
    req.add_header("X-Correlation-Id", CORS)
    if body is not None:
        req.add_header("Content-Type", "application/json")

    try:
        with urllib.request.urlopen(req, context=ssl._create_unverified_context()) as r:
            text = r.read().decode("utf-8")
            parsed = json.loads(text) if text else {}
            return r.status, parsed
    except urllib.error.HTTPError as ex:
        text = ex.read().decode("utf-8")
        parsed = json.loads(text) if text else {}
        return ex.code, parsed


def main():
    start1_status, start1 = call("POST", "/api/v1/ai/workflows/start", TOKEN_A, START_BODY)
    start2_status, start2 = call("POST", "/api/v1/ai/workflows/start", TOKEN_A, START_BODY)
    exec1 = start1.get("executionId")
    exec2 = start2.get("executionId")

    assist_status, assist = call("POST", "/api/v1/ai/assist", TOKEN_A, ASSIST_BODY)

    approve_status, _ = call("POST", f"/api/v1/ai/workflows/{exec1}/approve-step", TOKEN_A, {"reason": "approved-http-matrix"})
    reject_status, _ = call("POST", f"/api/v1/ai/workflows/{exec2}/reject-step", TOKEN_A, {"reason": "reject-http-matrix"})

    cross_tenant_status, _ = call("POST", f"/api/v1/ai/workflows/{exec1}/approve-step", TOKEN_B, {"reason": "cross-tenant-attempt"})

    start_timeout_status, start_timeout = call("POST", "/api/v1/ai/workflows/start", TOKEN_A, START_BODY)
    exec_timeout = start_timeout.get("executionId")
    time.sleep(2)
    timeout_approve_status, _ = call("POST", f"/api/v1/ai/workflows/{exec_timeout}/approve-step", TOKEN_A, {"reason": "late-approval"})

    get1_status, get1 = call("GET", f"/api/v1/ai/workflows/{exec1}", TOKEN_A)
    get2_status, get2 = call("GET", f"/api/v1/ai/workflows/{exec2}", TOKEN_A)
    get_timeout_status, get_timeout = call("GET", f"/api/v1/ai/workflows/{exec_timeout}", TOKEN_A)

    result = {
        "correlationId": CORS,
        "http": {
            "start1": start1_status,
            "start2": start2_status,
            "assist": assist_status,
            "approve": approve_status,
            "reject": reject_status,
            "crossTenantApprove": cross_tenant_status,
            "startTimeout": start_timeout_status,
            "timeoutApprove": timeout_approve_status,
            "get1": get1_status,
            "get2": get2_status,
            "getTimeout": get_timeout_status,
        },
        "executions": {
            "first": exec1,
            "retry": exec2,
            "timeout": exec_timeout,
        },
        "idempotencyRetryDifferentExecution": exec1 != exec2,
        "statuses": {
            "first": get1.get("status"),
            "retry": get2.get("status"),
            "timeout": get_timeout.get("status"),
        },
        "assistSerialization": {
            "hasConfidence": assist.get("confidenceScore") is not None,
            "hasReasoning": len(assist.get("reasoning", [])) > 0,
            "hasSources": len(assist.get("sourceReferences", [])) > 0,
        },
    }
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
