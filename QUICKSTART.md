# Quick Reference: Accessing CreateHost API URL

## TL;DR - Get Started in 30 Seconds

### Option 1: Using Environment Variable (Simplest)
```bash
export CREATE_HOST_API_URL="https://api.example.com/v1/hosts/create"
python3 create_host_example.py
```

### Option 2: Using Config File
```bash
# Edit config.json with your API URL
node create_host_example.js
```

## Quick Examples

### Python - Get the URL
```python
from create_host_example import CreateHostAPIClient

client = CreateHostAPIClient()
url = client.get_api_url()
print(f"CreateHost API URL: {url}")
```

### JavaScript - Get the URL
```javascript
const CreateHostAPIClient = require('./create_host_example.js');

const client = new CreateHostAPIClient();
const url = client.getApiUrl();
console.log(`CreateHost API URL: ${url}`);
```

## Environment Variables You Can Set

| Variable | Description | Example |
|----------|-------------|---------|
| `CREATE_HOST_API_URL` | Direct full URL | `https://api.example.com/v1/hosts/create` |
| `API_BASE_URL` | Base URL only | `https://api.example.com` |
| `CREATE_HOST_ENDPOINT` | Endpoint path only | `/v1/hosts/create` |
| `ENVIRONMENT` | Environment name | `development`, `staging`, `production` |

## Files in This Repository

| File | Purpose | When to Use |
|------|---------|-------------|
| `README.md` | Main documentation | Start here |
| `CreateHostAPI.md` | Detailed guide | For comprehensive examples |
| `create_host_example.py` | Python implementation | Copy/adapt for Python projects |
| `create_host_example.js` | JavaScript implementation | Copy/adapt for Node.js projects |
| `config.json` | Configuration template | For config-based setup |
| `.env.example` | Environment vars template | For env-based setup |

## Common Use Cases

### CI/CD Pipeline
```yaml
# GitHub Actions example
env:
  CREATE_HOST_API_URL: ${{ secrets.CREATE_HOST_API_URL }}
```

### Docker Container
```dockerfile
ENV CREATE_HOST_API_URL=https://api.example.com/v1/hosts/create
```

### Local Development
```bash
cp .env.example .env
# Edit .env with your local API URL
```

## Testing Different Environments

```bash
# Development
ENVIRONMENT=development python3 create_host_example.py

# Staging
ENVIRONMENT=staging python3 create_host_example.py

# Production
ENVIRONMENT=production python3 create_host_example.py
```

## Need More Help?

1. Read `README.md` for detailed instructions
2. Check `CreateHostAPI.md` for language-specific examples
3. Run the example scripts to see it in action
4. Adapt the code to your specific needs
