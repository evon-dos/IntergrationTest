# IntegrationTest - CreateHost API Access Guide

This repository demonstrates how to access and use the **CreateHost API URL** in integration tests.

## 📋 Quick Start

### Getting the CreateHost API URL

There are multiple ways to access the CreateHost API URL:

1. **Environment Variable** (Recommended for CI/CD)
   ```bash
   export CREATE_HOST_API_URL="https://api.example.com/v1/hosts/create"
   ```

2. **Configuration File**
   ```json
   {
     "api": {
       "baseUrl": "https://api.example.com",
       "endpoints": {
         "createHost": "/v1/hosts/create"
       }
     }
   }
   ```

3. **Programmatically in Code**
   - See example files: `create_host_example.py` and `create_host_example.js`

## 📚 Documentation

- **[CreateHostAPI.md](CreateHostAPI.md)** - Complete guide with examples in multiple languages
- **[config.json](config.json)** - Configuration file example
- **[.env.example](.env.example)** - Environment variables template

## 🚀 Usage Examples

### Python Example

```bash
# Set up environment (optional)
cp .env.example .env
# Edit .env with your API URL

# Run the example
python3 create_host_example.py
```

### JavaScript/Node.js Example

```bash
# Set up environment (optional)
cp .env.example .env
# Edit .env with your API URL

# Run the example
node create_host_example.js
```

## 🎯 Different Methods to Access the URL

### Method 1: Direct Environment Variable

```bash
export CREATE_HOST_API_URL="https://api.example.com/v1/hosts/create"
```

Then in your code:
- Python: `os.environ.get('CREATE_HOST_API_URL')`
- JavaScript: `process.env.CREATE_HOST_API_URL`
- C#: `Environment.GetEnvironmentVariable("CREATE_HOST_API_URL")`

### Method 2: Composite URL (Base + Endpoint)

```bash
export API_BASE_URL="https://api.example.com"
export CREATE_HOST_ENDPOINT="/v1/hosts/create"
```

Combine them in your code to build the full URL.

### Method 3: Configuration File

Edit `config.json` with your API details:
```json
{
  "api": {
    "baseUrl": "https://your-api.example.com",
    "endpoints": {
      "createHost": "/v1/hosts/create"
    }
  }
}
```

### Method 4: Environment-Specific Configuration

The `config.json` supports multiple environments:
```bash
export ENVIRONMENT=production  # or development, staging
```

The code will automatically use the correct base URL for that environment.

## 💡 Key Features

- ✅ Multiple URL resolution strategies
- ✅ Environment-specific configurations
- ✅ Fallback mechanisms
- ✅ URL validation
- ✅ Examples in Python and JavaScript
- ✅ Best practices and patterns

## 📖 Files in This Repository

| File | Description |
|------|-------------|
| `CreateHostAPI.md` | Comprehensive documentation with examples in multiple languages |
| `create_host_example.py` | Python example demonstrating URL access |
| `create_host_example.js` | JavaScript/Node.js example demonstrating URL access |
| `config.json` | Configuration file with API endpoints |
| `.env.example` | Template for environment variables |

## 🔧 Testing the Examples

### Python

```bash
# Using environment variable
CREATE_HOST_API_URL="https://api.example.com/v1/hosts/create" python3 create_host_example.py

# Using config file
python3 create_host_example.py

# Using specific environment
ENVIRONMENT=staging python3 create_host_example.py
```

### JavaScript

```bash
# Using environment variable
CREATE_HOST_API_URL="https://api.example.com/v1/hosts/create" node create_host_example.js

# Using config file
node create_host_example.js

# Using specific environment
ENVIRONMENT=staging node create_host_example.js
```

## 📝 Output Example

When you run the examples, you'll see:

```
CreateHost API URL Access Example
------------------------------------------------------------
✓ Using CREATE_HOST_API_URL from environment: https://api.example.com/v1/hosts/create

============================================================
CreateHost API URL Information
============================================================
Full URL:    https://api.example.com/v1/hosts/create
Scheme:      https
Host:        api.example.com
Path:        /v1/hosts/create
Timeout:     30s
============================================================

✓ URL is valid: https://api.example.com/v1/hosts/create

📡 Making request to: https://api.example.com/v1/hosts/create
📦 Payload: {
  "hostname": "test-server-001",
  "ip_address": "192.168.1.100",
  "description": "Test server for integration testing",
  "tags": ["test", "integration"]
}

💡 To get just the URL in your code:
   url = client.get_api_url()
   # Returns: https://api.example.com/v1/hosts/create
```

## 🎓 Best Practices

1. **Use environment variables** for different deployment environments
2. **Implement fallback mechanisms** (env vars → config file → defaults)
3. **Validate URLs** before making API calls
4. **Log URL access** (without sensitive data) for debugging
5. **Use URL builders** to construct complex URLs safely

## 🤝 Contributing

Feel free to add more examples in other programming languages or improve existing documentation.

## 📄 License

This is a demonstration repository for integration testing patterns.