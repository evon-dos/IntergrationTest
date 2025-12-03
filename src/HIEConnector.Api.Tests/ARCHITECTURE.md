# Test Factory Architecture Diagram

## Factory Pattern Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Test Execution Flow                          │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────┐
│   Test Class     │
│  (xUnit Test)    │
└────────┬─────────┘
         │
         │ implements IClassFixture<T>
         │
         ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    TestWebApplicationFactory                         │
│  (extends WebApplicationFactory<Program>)                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │            ConfigureWebHost Override                        │   │
│  │                                                               │   │
│  │  1. Start Test Containers (PostgreSQL, Redis)               │   │
│  │  2. Override Configuration                                   │   │
│  │  3. Replace Services with Test Versions                     │   │
│  │  4. Configure Test Database                                  │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                       │
│  Helper Methods:                                                     │
│  - SeedTestDataAsync()                                               │
│  - CleanupDatabaseAsync()                                            │
│  - GetService<T>()                                                   │
└───────────────────────────┬───────────────────────────────────────────┘
                            │
                            │ creates
                            ▼
                  ┌──────────────────┐
                  │  Test Server     │
                  │  (In-Memory)     │
                  └────────┬─────────┘
                           │
                           │ exposes
                           ▼
                  ┌──────────────────┐
                  │   HttpClient     │
                  │  (Test Client)   │
                  └──────────────────┘
```

## Component Interaction

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Test Infrastructure                           │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│  PostgreSQL  │         │    Redis     │         │  Test Mocks  │
│  Container   │         │  Container   │         │  (Services)  │
└──────┬───────┘         └──────┬───────┘         └──────┬───────┘
       │                        │                        │
       │ connection             │ connection             │ injected
       │                        │                        │
       ▼                        ▼                        ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         Test Server                                  │
│                    (ASP.NET Core TestHost)                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌─────────────────────┐  ┌─────────────────────┐                  │
│  │   DbContext         │  │   SignalR Hub       │                  │
│  │   (Test Database)   │  │   (Test Redis)      │                  │
│  └─────────────────────┘  └─────────────────────┘                  │
│                                                                       │
│  ┌─────────────────────┐  ┌─────────────────────┐                  │
│  │  Controllers        │  │   Services          │                  │
│  │  (Real)             │  │   (Mocked/Real)     │                  │
│  └─────────────────────┘  └─────────────────────┘                  │
│                                                                       │
└────────────────────────────┬──────────────────────────────────────────┘
                             │
                             │ HTTP Requests
                             ▼
                    ┌──────────────────┐
                    │   Test Client    │
                    │   (HttpClient)   │
                    └────────┬─────────┘
                             │
                             │ used by
                             ▼
                    ┌──────────────────┐
                    │   Test Methods   │
                    │   (xUnit Facts)  │
                    └──────────────────┘
```

## Service Replacement Strategy

```
┌─────────────────────────────────────────────────────────────────────┐
│                   Production vs Test Services                        │
└─────────────────────────────────────────────────────────────────────┘

Production:                          Test:
┌──────────────────┐                ┌──────────────────┐
│   PostgreSQL     │                │   PostgreSQL     │
│   (Real Server)  │────────X──────▶│   Container      │
└──────────────────┘                └──────────────────┘
                                              OR
                                    ┌──────────────────┐
                                    │   In-Memory DB   │
                                    └──────────────────┘

┌──────────────────┐                ┌──────────────────┐
│   Redis          │                │   Redis          │
│   (Real Cluster) │────────X──────▶│   Container      │
└──────────────────┘                └──────────────────┘
                                              OR
                                    ┌──────────────────┐
                                    │   In-Memory      │
                                    │   Backplane      │
                                    └──────────────────┘

┌──────────────────┐                ┌──────────────────┐
│  SmileCDR Client │                │  Mock SmileCDR   │
│  (External API)  │────────X──────▶│  Client          │
└──────────────────┘                └──────────────────┘

┌──────────────────┐                ┌──────────────────┐
│  Telemetry       │                │  No-Op or Mock   │
│  (OTLP Export)   │────────X──────▶│  Telemetry       │
└──────────────────┘                └──────────────────┘
```

## Test Lifecycle

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Test Lifecycle Phases                           │
└─────────────────────────────────────────────────────────────────────┘

1. Test Collection Starts
   │
   ├─▶ Factory.InitializeAsync()
   │   │
   │   ├─▶ Start PostgreSQL Container
   │   ├─▶ Start Redis Container
   │   └─▶ Create Test Server
   │
   ▼

2. Before Each Test
   │
   ├─▶ Test.InitializeAsync()
   │   │
   │   ├─▶ CleanupDatabaseAsync()
   │   │   └─▶ Drop and Recreate Schema
   │   │
   │   └─▶ SeedTestDataAsync() (if needed)
   │       └─▶ Insert Test Data
   │
   ▼

3. Test Execution
   │
   ├─▶ Arrange: Setup test conditions
   ├─▶ Act: Execute HTTP requests
   └─▶ Assert: Verify results
   │
   ▼

4. After Each Test
   │
   └─▶ Test.DisposeAsync()
       └─▶ Cleanup (optional)
   │
   ▼

5. Test Collection Ends
   │
   └─▶ Factory.DisposeAsync()
       │
       ├─▶ Stop PostgreSQL Container
       ├─▶ Stop Redis Container
       └─▶ Dispose Test Server
```

## Factory Types Comparison

```
┌─────────────────────────────────────────────────────────────────────┐
│              Full Factory vs Simple Factory                          │
└─────────────────────────────────────────────────────────────────────┘

TestWebApplicationFactory          SimpleTestWebApplicationFactory
(Full Integration)                 (Lightweight)

┌─────────────────────┐           ┌─────────────────────┐
│ Docker Containers   │           │ No Docker           │
│ - PostgreSQL        │           │                     │
│ - Redis             │           │                     │
└─────────────────────┘           └─────────────────────┘
         │                                  │
         ▼                                  ▼
┌─────────────────────┐           ┌─────────────────────┐
│ Real DB & Cache     │           │ In-Memory DB        │
│ - Migrations Work   │           │ - Fast Setup        │
│ - Real Queries      │           │ - No Migrations     │
└─────────────────────┘           └─────────────────────┘
         │                                  │
         ▼                                  ▼
┌─────────────────────┐           ┌─────────────────────┐
│ Slower Tests        │           │ Faster Tests        │
│ Higher Confidence   │           │ Lower Confidence    │
│ CI/CD: Complex      │           │ CI/CD: Simple       │
└─────────────────────┘           └─────────────────────┘

Use Case:                          Use Case:
- End-to-End Tests                 - Unit-like Tests
- Pre-Production                   - Development
- Critical Paths                   - Fast Feedback
```

## Integration Test Base Class Pattern

```
┌─────────────────────────────────────────────────────────────────────┐
│                 IntegrationTestBase Pattern                          │
└─────────────────────────────────────────────────────────────────────┘

                    IntegrationTestFixture
                            │
                            │ provides
                            ▼
┌───────────────────────────────────────────────────────────┐
│                 IntegrationTestBase                        │
│                 (Abstract Base Class)                      │
├───────────────────────────────────────────────────────────┤
│  Properties:                                               │
│  - Factory (TestWebApplicationFactory)                     │
│  - Client (HttpClient)                                     │
│                                                             │
│  Methods:                                                  │
│  - InitializeAsync() → CleanupDatabaseAsync()             │
│  - SeedAsync(action) → SeedTestDataAsync()                │
│  - GetService<T>() → Resolve from DI                      │
│  - CreateClient() → New HttpClient                        │
└───────────────────────────────────────────────────────────┘
                            │
                            │ inherited by
                            ▼
        ┌──────────────────────────────────────┐
        │                                       │
┌───────▼─────────┐              ┌─────────────▼──────┐
│  TenantTests    │              │  MessageTests      │
│                 │              │                    │
│  [Fact]         │              │  [Fact]            │
│  TestCreate()   │              │  TestSend()        │
│  TestUpdate()   │              │  TestReceive()     │
└─────────────────┘              └────────────────────┘

Benefits:
- Reduced Boilerplate
- Consistent Test Patterns
- Shared Setup/Teardown
- Helper Methods Available
```
