# Architecture Diagram

## Integration Test Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Integration Test Runner                       │
│                         (xUnit / dotnet test)                    │
└───────────────────────┬─────────────────────────────────────────┘
                        │
                        │ Creates
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│          SignalRWebApplicationFactory (Test Fixture)             │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  1. Starts TestContainer (Redis Docker Container)         │  │
│  │  2. Overrides appsettings with Redis connection string    │  │
│  │  3. Hosts Web API in-memory via WebApplicationFactory     │  │
│  └───────────────────────────────────────────────────────────┘  │
└───────────┬──────────────────────────────────────┬──────────────┘
            │                                      │
            │ Provides                             │ Provides
            │ Test Server                          │ Redis Connection
            ▼                                      ▼
┌───────────────────────────┐        ┌──────────────────────────┐
│   In-Memory Web API       │        │   Redis Container        │
│   ┌───────────────────┐   │        │   (TestContainers)       │
│   │  Program.cs       │   │        │                          │
│   │  ├─ SignalR       │   │◄───────┤  Port: 6379 (mapped)     │
│   │  ├─ Redis Config  │   │        │  Image: redis:7-alpine   │
│   │  └─ ChatHub       │   │        │  Auto cleanup on exit    │
│   └───────────────────┘   │        └──────────────────────────┘
└───────────┬───────────────┘
            │
            │ Test Server Handler
            │ (No real HTTP calls)
            ▼
┌───────────────────────────────────────────────────────────────┐
│                    SignalR Client Connections                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐   │
│  │ Connection 1 │    │ Connection 2 │    │ Connection N │   │
│  │              │    │              │    │              │   │
│  │ • Subscribe  │    │ • Subscribe  │    │ • Subscribe  │   │
│  │ • Invoke     │    │ • Invoke     │    │ • Invoke     │   │
│  │ • Receive    │    │ • Receive    │    │ • Receive    │   │
│  └──────────────┘    └──────────────┘    └──────────────┘   │
└───────────────────────────────────────────────────────────────┘
```

## Test Execution Flow

```
1. Test Class Constructor
   └─► SignalRWebApplicationFactory.InitializeAsync()
       └─► RedisContainer.StartAsync()
           └─► Docker pulls/starts Redis container
   
2. Test Method Starts
   └─► Create HttpClient from factory
   └─► Build SignalR HubConnection with test server handler
   └─► Start SignalR connection
   
3. Test Execution
   └─► SignalR Client 1 sends message
       └─► In-Memory API receives message
           └─► ChatHub processes message
               └─► Publishes to Redis backplane
                   └─► Redis broadcasts to all subscribers
                       └─► SignalR Client 2 receives message
   
4. Test Cleanup
   └─► Stop SignalR connections
   └─► SignalRWebApplicationFactory.DisposeAsync()
       └─► RedisContainer.StopAsync()
           └─► Docker stops and removes container
```

## Key Components Interaction

```
┌─────────────────────────────────────────────────────────────────┐
│                         Test Project                             │
│                                                                  │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  ChatHubIntegrationTests                               │    │
│  │  ├─ SendMessage_ShouldBroadcastToAllConnectedClients   │    │
│  │  ├─ JoinGroup_ShouldReceiveGroupMessages               │    │
│  │  ├─ LeaveGroup_ShouldNotReceiveGroupMessages           │    │
│  │  ├─ OnConnectedAsync_ShouldNotifyAllClients            │    │
│  │  └─ Connections_ShouldBeAbleToReconnect                │    │
│  └────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              │ Uses                              │
│                              ▼                                   │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  SignalRWebApplicationFactory                          │    │
│  │                                                         │    │
│  │  protected override void ConfigureWebHost()            │    │
│  │  {                                                      │    │
│  │      builder.ConfigureAppConfiguration((ctx, cfg) =>   │    │
│  │      {                                                  │    │
│  │          cfg.AddInMemoryCollection(new Dictionary      │    │
│  │          {                                              │    │
│  │              ["ConnectionStrings:Redis"] =             │    │
│  │                  RedisConnectionString                  │    │
│  │          });                                            │    │
│  │      });                                                │    │
│  │  }                                                      │    │
│  └────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              │ Hosts                             │
│                              ▼                                   │
└──────────────────────────────┼───────────────────────────────────┘
                               │
┌──────────────────────────────┼───────────────────────────────────┐
│                         Web API Project                          │
│                              │                                   │
│                              ▼                                   │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  Program.cs                                             │    │
│  │                                                         │    │
│  │  var signalRBuilder = services.AddSignalR();           │    │
│  │                                                         │    │
│  │  if (!string.IsNullOrEmpty(redisConnectionString))     │    │
│  │  {                                                      │    │
│  │      signalRBuilder.AddStackExchangeRedis(             │    │
│  │          redisConnectionString,                         │    │
│  │          options =>                                     │    │
│  │          {                                              │    │
│  │              options.Configuration.ChannelPrefix =     │    │
│  │                  RedisChannel.Literal("SignalRWebApi"); │    │
│  │          });                                            │    │
│  │  }                                                      │    │
│  │                                                         │    │
│  │  app.MapHub<ChatHub>("/chathub");                      │    │
│  └────────────────────────────────────────────────────────┘    │
│                              │                                   │
│                              │ Uses                              │
│                              ▼                                   │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  ChatHub                                                │    │
│  │  ├─ SendMessage(user, message)                         │    │
│  │  ├─ SendMessageToGroup(group, user, message)           │    │
│  │  ├─ JoinGroup(groupName)                               │    │
│  │  ├─ LeaveGroup(groupName)                              │    │
│  │  ├─ OnConnectedAsync()                                 │    │
│  │  └─ OnDisconnectedAsync()                              │    │
│  └────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

## Message Flow Example

```
┌──────────┐                                        ┌──────────┐
│ Client 1 │                                        │ Client 2 │
└────┬─────┘                                        └────┬─────┘
     │                                                   │
     │ 1. SendMessage("Alice", "Hello")                 │
     │                                                   │
     ▼                                                   │
┌─────────────────────────┐                             │
│   SignalR Hub           │                             │
│   ChatHub.SendMessage() │                             │
└────────────┬────────────┘                             │
             │                                           │
             │ 2. Clients.All.SendAsync()               │
             │                                           │
             ▼                                           │
┌─────────────────────────┐                             │
│   Redis Backplane       │                             │
│   Pub/Sub Channel       │                             │
└────────────┬────────────┘                             │
             │                                           │
             │ 3. Broadcast to all subscribers          │
             │                                           │
             ├───────────────────┐                      │
             │                   │                      │
             ▼                   ▼                      │
     ┌──────────┐        ┌──────────┐                  │
     │ Client 1 │        │ Client 2 │                  │
     │ Hub Conn │        │ Hub Conn │                  │
     └────┬─────┘        └────┬─────┘                  │
          │                   │                         │
          │                   │ 4. ReceiveMessage()     │
          │                   │                         │
          │                   └─────────────────────────┘
          │                                             │
          │ 5. Test assertion                           ▼
          │    verifies message                   "Alice: Hello"
          │    received correctly
          │
```

## Benefits Visualization

```
┌───────────────────────────────────────────────────────────────┐
│  Traditional Approach          │  This Implementation          │
├────────────────────────────────┼───────────────────────────────┤
│  ❌ Manual Redis setup         │  ✅ Automatic Redis via       │
│     required                   │     TestContainers            │
│                                │                               │
│  ❌ External dependencies      │  ✅ Self-contained tests      │
│                                │                               │
│  ❌ Real HTTP networking       │  ✅ In-memory test server     │
│     overhead                   │     (faster)                  │
│                                │                               │
│  ❌ Shared test environment    │  ✅ Isolated test instances   │
│     (port conflicts)           │                               │
│                                │                               │
│  ❌ Manual cleanup required    │  ✅ Automatic cleanup via     │
│                                │     IAsyncLifetime            │
│                                │                               │
│  ❌ CI/CD requires Redis       │  ✅ CI/CD only needs Docker   │
│     service configuration      │                               │
└────────────────────────────────┴───────────────────────────────┘
```
