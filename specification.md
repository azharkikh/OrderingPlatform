# ROLE AND CONTEXT
You are a Principal DevOps Engineer and Enterprise .NET Architect. You are an expert in containerized integration testing, GitHub Actions pipelines, and dynamic out-of-process code coverage profiling for running processes.

Your task is to provide a complete, production-ready implementation for gathering code coverage from a running .NET 10 Web API container (running on Ubuntu) while it is being tested by integration tests executing from a separate container inside GitHub Actions.

---

# ARCHITECTURAL PARADIGM
* **App Container:** Runs the live .NET 10 Web API. The main process must be dynamically profiled using Microsoft's `dotnet-coverage` tool installed as a global tool.
* **Test Container:** Runs integration/E2E tests that interact with the App Container via HTTP/API.
* **Orchestration:** Docker Compose manages both services within GitHub Actions (`ubuntu-latest` runner).

---

# SYSTEM CAPABILITIES & INSTRUCTIONS (FOR OPUS 4.8)
1. **Adaptive Thinking Mode:** Maximize your reasoning budget. Analyze .NET 10 specifics and process lifecycle management inside Linux containers.
2. **No Placeholders:** All Dockerfiles, Docker Compose files, and scripts must be 100% complete and ready to copy-paste.
3. **Strict Focus on Out-of-Process:** Do NOT use `dotnet test` inside the application container. The app must run normally as a web service but under the `dotnet-coverage` orchestrator.

---

# TECHNICAL ENVIRONMENT
* **Base OS in Containers:** Ubuntu-based official Microsoft images (e.g., `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0`).
* **Runtime/SDK:** .NET 10.0
* **Coverage Tooling:** `dotnet-coverage` (global tool)
* **Orchestration:** Docker Compose + GitHub Actions (`ubuntu-latest`)
* **Shared Output:** A host-mapped Docker volume to extract the final `coverage.cobertura.xml` report.

---

# CORE TASKS & REQUIREMENTS FOR THE SOLUTION

### 1. Application Dockerfile (Ubuntu + .NET 10 + Profiler)
* Create a multi-stage `Dockerfile` for the Web API based on .NET 10 Ubuntu images.
* In the final stage, install `dotnet-coverage` globally.
* Configure the `ENTRYPOINT` to launch the API via the collector. Ensure environment variables for the ASP.NET Core URLs are properly passed.
  * Example pattern: `dotnet-coverage collect "dotnet MyApp.dll" --output /output/coverage.cobertura.xml --format cobertura`

### 2. Docker Compose & Graceful Shutdown Guardrails
* Create a `docker-compose.yml` connecting the API and Test services.
* Configure a proper **Healthcheck** for the API container so the Test container waits for `service_healthy`.
* **Handling SIGTERM (CRITICAL):** Provide a bulletproof mechanism to ensure that when tests finish, the API container receives a `SIGTERM` signal, gracefully flushes the buffered coverage data to disk, and closes the XML file cleanly before the container exits. 

### 3. GitHub Actions CI Pipeline
* Provide the complete `.github/workflows/integration-tests.yml` utilizing the `ubuntu-latest` runner.
* The workflow must:
  1. Spin up the orchestration using `docker compose up --exit-code-from <test-service>`.
  2. Verify that the coverage file was successfully written to the mapped volume.
  3. Use `actions/upload-artifact` to archive the `coverage.cobertura.xml` for downstream step processing (like SonarQube or Codecov).

---

# EXPECTED OUTPUT FORMAT
1. **Lifecycle & Signal Flow Analysis:** Briefly explain how .NET 10 under `dotnet-coverage` reacts to `SIGTERM` and ensures zero data loss.
2. **The Codebase:**
   * `Dockerfile.app` (Web API with .NET 10 SDK/Runtime on Ubuntu).
   * `Dockerfile.tests` (Test runner container).
   * `docker-compose.yml` (With dependencies, healthchecks, and volume mappings).
   * `integration-tests.yml` (GitHub Actions workflow).
3. **Troubleshooting:** One-liner fixes for common issues in .NET 10 container profiling (e.g., permission issues on the mapped `/output` folder inside Ubuntu).