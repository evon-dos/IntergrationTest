/**
 * CreateHost API Integration Test Example (JavaScript/Node.js)
 * 
 * This module demonstrates how to access and use the CreateHost API URL
 * in integration tests using JavaScript/Node.js.
 */

const fs = require('fs');
const path = require('path');

class CreateHostAPIClient {
    /**
     * Initialize the API client with URL from multiple sources
     * 
     * Priority order:
     * 1. Environment variable CREATE_HOST_API_URL
     * 2. Environment variables API_BASE_URL + CREATE_HOST_ENDPOINT
     * 3. Configuration file
     * 4. Default localhost
     */
    constructor(configPath = 'config.json') {
        this.apiUrl = this._resolveApiUrl(configPath);
        this.timeout = parseInt(process.env.API_TIMEOUT || '30000', 10);
    }

    /**
     * Resolve the CreateHost API URL from various sources
     * 
     * @returns {string} The full URL for the CreateHost API endpoint
     */
    _resolveApiUrl(configPath) {
        // Method 1: Direct environment variable
        let apiUrl = process.env.CREATE_HOST_API_URL;
        if (apiUrl) {
            console.log(`✓ Using CREATE_HOST_API_URL from environment: ${apiUrl}`);
            return apiUrl;
        }

        // Method 2: Composite from base URL + endpoint
        const baseUrl = process.env.API_BASE_URL;
        const endpoint = process.env.CREATE_HOST_ENDPOINT;
        if (baseUrl && endpoint) {
            apiUrl = new URL(endpoint, baseUrl).toString();
            console.log(`✓ Using API_BASE_URL + CREATE_HOST_ENDPOINT: ${apiUrl}`);
            return apiUrl;
        }

        // Method 3: Load from configuration file
        try {
            const configData = fs.readFileSync(configPath, 'utf8');
            const config = JSON.parse(configData);

            // Check for environment-specific configuration
            const env = process.env.ENVIRONMENT || 'development';
            let resolvedBaseUrl;
            
            if (config.environments && config.environments[env]) {
                resolvedBaseUrl = config.environments[env].baseUrl;
            } else {
                resolvedBaseUrl = config.api.baseUrl;
            }

            const resolvedEndpoint = config.api.endpoints.createHost;
            apiUrl = new URL(resolvedEndpoint, resolvedBaseUrl).toString();
            console.log(`✓ Using configuration file (${env}): ${apiUrl}`);
            return apiUrl;
        } catch (error) {
            console.log(`⚠ Warning: Could not load config file: ${error.message}`);
        }

        // Method 4: Default fallback
        apiUrl = 'http://localhost:8080/v1/hosts/create';
        console.log(`✓ Using default URL: ${apiUrl}`);
        return apiUrl;
    }

    /**
     * Get the CreateHost API URL
     * 
     * @returns {string} The full URL for the CreateHost API endpoint
     */
    getApiUrl() {
        return this.apiUrl;
    }

    /**
     * Validate that the URL is properly formatted
     * 
     * @returns {boolean} True if URL is valid, False otherwise
     */
    validateUrl() {
        try {
            const url = new URL(this.apiUrl);
            const isValid = !!(url.protocol && url.hostname);
            if (isValid) {
                console.log(`✓ URL is valid: ${this.apiUrl}`);
            } else {
                console.log(`✗ URL is invalid: ${this.apiUrl}`);
            }
            return isValid;
        } catch (error) {
            console.log(`✗ URL validation error: ${error.message}`);
            return false;
        }
    }

    /**
     * Create a new host using the API
     * 
     * @param {Object} hostData - Object containing host information
     * @returns {Promise<Object>} API response
     */
    async createHost(hostData) {
        const url = this.getApiUrl();
        console.log(`\n📡 Making request to: ${url}`);
        console.log(`📦 Payload: ${JSON.stringify(hostData, null, 2)}`);

        // Note: This is a demonstration. In a real implementation,
        // you would use axios or fetch:
        // const response = await axios.post(url, hostData, { timeout: this.timeout });
        // return response.data;

        // For demonstration purposes, return a mock response
        return {
            status: 'success',
            message: 'Host would be created at this URL',
            url: url,
            data: hostData
        };
    }

    /**
     * Print detailed information about the API URL
     */
    printUrlInfo() {
        const url = new URL(this.apiUrl);
        console.log('\n' + '='.repeat(60));
        console.log('CreateHost API URL Information');
        console.log('='.repeat(60));
        console.log(`Full URL:    ${this.apiUrl}`);
        console.log(`Protocol:    ${url.protocol}`);
        console.log(`Host:        ${url.hostname}`);
        console.log(`Port:        ${url.port || 'default'}`);
        console.log(`Path:        ${url.pathname}`);
        console.log(`Timeout:     ${this.timeout}ms`);
        console.log('='.repeat(60) + '\n');
    }
}

/**
 * Main function demonstrating API URL access
 */
async function main() {
    console.log('CreateHost API URL Access Example');
    console.log('-'.repeat(60));

    // Initialize the API client
    const client = new CreateHostAPIClient();

    // Display URL information
    client.printUrlInfo();

    // Validate the URL
    if (!client.validateUrl()) {
        console.log('⚠ Warning: URL validation failed');
        process.exit(1);
    }

    // Example: Create a host
    const hostData = {
        hostname: 'test-server-001',
        ip_address: '192.168.1.100',
        description: 'Test server for integration testing',
        tags: ['test', 'integration']
    };

    const result = await client.createHost(hostData);
    console.log(`\n✓ Result: ${JSON.stringify(result, null, 2)}`);

    // Show how to get just the URL
    console.log(`\n💡 To get just the URL in your code:`);
    console.log(`   const url = client.getApiUrl();`);
    console.log(`   // Returns: ${client.getApiUrl()}`);
}

// Run if executed directly
if (require.main === module) {
    main().catch(error => {
        console.error('Error:', error);
        process.exit(1);
    });
}

module.exports = CreateHostAPIClient;
