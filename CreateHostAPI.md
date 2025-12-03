# How to Access the CreateHost API URL

This guide explains different methods to access and use the CreateHost API URL in your integration tests.

## Table of Contents
1. [Configuration-based Access](#configuration-based-access)
2. [Environment Variable Access](#environment-variable-access)
3. [Programmatic Access](#programmatic-access)
4. [Example Usage](#example-usage)

---

## Configuration-based Access

### Using Configuration Files

Create a configuration file to store your API URLs:

**config.json**
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

**Access in Code:**
```javascript
// JavaScript/Node.js
const config = require('./config.json');
const createHostUrl = `${config.api.baseUrl}${config.api.endpoints.createHost}`;
console.log('CreateHost API URL:', createHostUrl);
```

```python
# Python
import json

with open('config.json', 'r') as f:
    config = json.load(f)
    
create_host_url = f"{config['api']['baseUrl']}{config['api']['endpoints']['createHost']}"
print(f'CreateHost API URL: {create_host_url}')
```

```csharp
// C#
using System.Text.Json;

var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
var createHostUrl = $"{config.Api.BaseUrl}{config.Api.Endpoints.CreateHost}";
Console.WriteLine($"CreateHost API URL: {createHostUrl}");
```

---

## Environment Variable Access

### Setting Environment Variables

Set the CreateHost API URL as an environment variable:

**Linux/Mac:**
```bash
export CREATE_HOST_API_URL="https://api.example.com/v1/hosts/create"
```

**Windows:**
```cmd
set CREATE_HOST_API_URL=https://api.example.com/v1/hosts/create
```

**Docker/Docker Compose:**
```yaml
environment:
  - CREATE_HOST_API_URL=https://api.example.com/v1/hosts/create
```

### Accessing in Code

```javascript
// JavaScript/Node.js
const createHostUrl = process.env.CREATE_HOST_API_URL;
console.log('CreateHost API URL:', createHostUrl);
```

```python
# Python
import os

create_host_url = os.environ.get('CREATE_HOST_API_URL')
print(f'CreateHost API URL: {create_host_url}')
```

```csharp
// C#
var createHostUrl = Environment.GetEnvironmentVariable("CREATE_HOST_API_URL");
Console.WriteLine($"CreateHost API URL: {createHostUrl}");
```

```java
// Java
String createHostUrl = System.getenv("CREATE_HOST_API_URL");
System.out.println("CreateHost API URL: " + createHostUrl);
```

---

## Programmatic Access

### Service Discovery

If using service discovery (like Consul, Eureka, etc.):

```javascript
// JavaScript with service discovery
const serviceRegistry = require('./serviceRegistry');

async function getCreateHostUrl() {
    const service = await serviceRegistry.discover('host-service');
    return `${service.url}/create`;
}

getCreateHostUrl().then(url => {
    console.log('CreateHost API URL:', url);
});
```

### API Client with URL Management

```python
# Python API Client
class HostAPIClient:
    def __init__(self, base_url):
        self.base_url = base_url
        self.create_host_endpoint = '/v1/hosts/create'
    
    def get_create_host_url(self):
        """Returns the full URL for CreateHost API"""
        return f"{self.base_url}{self.create_host_endpoint}"
    
    def create_host(self, host_data):
        url = self.get_create_host_url()
        # Make API call using the URL
        return requests.post(url, json=host_data)

# Usage
client = HostAPIClient('https://api.example.com')
create_host_url = client.get_create_host_url()
print(f'CreateHost API URL: {create_host_url}')
```

---

## Example Usage

### Complete Integration Test Example

```python
import os
import requests
import json

class CreateHostIntegrationTest:
    """Integration test for CreateHost API"""
    
    def __init__(self):
        # Method 1: Environment variable (recommended for CI/CD)
        self.api_url = os.environ.get('CREATE_HOST_API_URL')
        
        # Method 2: Fallback to config file
        if not self.api_url:
            with open('config.json', 'r') as f:
                config = json.load(f)
                self.api_url = f"{config['api']['baseUrl']}{config['api']['endpoints']['createHost']}"
        
        # Method 3: Default for local development
        if not self.api_url:
            self.api_url = 'http://localhost:8080/v1/hosts/create'
    
    def get_api_url(self):
        """Get the CreateHost API URL"""
        return self.api_url
    
    def test_create_host(self):
        """Test creating a host using the API"""
        url = self.get_api_url()
        print(f'Testing CreateHost API at: {url}')
        
        payload = {
            'hostname': 'test-host-001',
            'ip_address': '192.168.1.100',
            'description': 'Test host for integration testing'
        }
        
        response = requests.post(url, json=payload)
        print(f'Response Status: {response.status_code}')
        print(f'Response Body: {response.json()}')
        
        return response.status_code == 201

# Usage
if __name__ == '__main__':
    test = CreateHostIntegrationTest()
    print(f'CreateHost API URL: {test.get_api_url()}')
    # Uncomment to run the actual test:
    # test.test_create_host()
```

### JavaScript/Node.js Example

```javascript
const axios = require('axios');

class CreateHostIntegrationTest {
    constructor() {
        // Method 1: Environment variable
        this.apiUrl = process.env.CREATE_HOST_API_URL;
        
        // Method 2: Fallback to config
        if (!this.apiUrl) {
            const config = require('./config.json');
            this.apiUrl = `${config.api.baseUrl}${config.api.endpoints.createHost}`;
        }
        
        // Method 3: Default
        if (!this.apiUrl) {
            this.apiUrl = 'http://localhost:8080/v1/hosts/create';
        }
    }
    
    getApiUrl() {
        return this.apiUrl;
    }
    
    async testCreateHost() {
        const url = this.getApiUrl();
        console.log(`Testing CreateHost API at: ${url}`);
        
        const payload = {
            hostname: 'test-host-001',
            ip_address: '192.168.1.100',
            description: 'Test host for integration testing'
        };
        
        try {
            const response = await axios.post(url, payload);
            console.log(`Response Status: ${response.status}`);
            console.log(`Response Body:`, response.data);
            return response.status === 201;
        } catch (error) {
            console.error('Error:', error.message);
            return false;
        }
    }
}

// Usage
const test = new CreateHostIntegrationTest();
console.log(`CreateHost API URL: ${test.getApiUrl()}`);
// Uncomment to run the actual test:
// test.testCreateHost();

module.exports = CreateHostIntegrationTest;
```

---

## Best Practices

1. **Use Environment Variables for Different Environments:**
   - `CREATE_HOST_API_URL` for production
   - `CREATE_HOST_API_URL_STAGING` for staging
   - `CREATE_HOST_API_URL_DEV` for development

2. **Implement Fallback Mechanisms:**
   - Try environment variables first
   - Fall back to configuration files
   - Use defaults for local development

3. **Validate URLs:**
   ```python
   from urllib.parse import urlparse
   
   def validate_url(url):
       try:
           result = urlparse(url)
           return all([result.scheme, result.netloc])
       except:
           return False
   ```

4. **Log URL Access (without sensitive data):**
   ```python
   import logging
   
   logging.info(f"Accessing CreateHost API at: {parsed_url.netloc}{parsed_url.path}")
   ```

5. **Use URL builders:**
   ```python
   from urllib.parse import urljoin
   
   base_url = "https://api.example.com"
   endpoint = "/v1/hosts/create"
   full_url = urljoin(base_url, endpoint)
   ```

---

## Troubleshooting

### URL Not Found
- Verify environment variables are set correctly
- Check configuration file path and format
- Ensure network connectivity to the API server

### Connection Refused
- Verify the API service is running
- Check firewall and network settings
- Validate the URL format (http vs https)

### Authentication Issues
- Ensure API keys/tokens are included in requests
- Check if URL requires authentication headers
- Verify credentials are valid

---

## Additional Resources

- API Documentation: Check your API provider's documentation
- Swagger/OpenAPI: If available, access at `/swagger` or `/api-docs`
- Health Check: Verify API availability at `/health` endpoint
