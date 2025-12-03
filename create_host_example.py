#!/usr/bin/env python3
"""
CreateHost API Integration Test Example

This module demonstrates how to access and use the CreateHost API URL
in integration tests using Python.
"""

import os
import json
import sys
from typing import Optional, Dict
from urllib.parse import urljoin, urlparse


class CreateHostAPIClient:
    """Client for interacting with the CreateHost API"""
    
    def __init__(self, config_path: str = 'config.json'):
        """
        Initialize the API client with URL from multiple sources
        
        Priority order:
        1. Environment variable CREATE_HOST_API_URL
        2. Environment variables API_BASE_URL + CREATE_HOST_ENDPOINT
        3. Configuration file
        4. Default localhost
        """
        self.api_url = self._resolve_api_url(config_path)
        self.timeout = int(os.environ.get('API_TIMEOUT', 30))
        
    def _resolve_api_url(self, config_path: str) -> str:
        """
        Resolve the CreateHost API URL from various sources
        
        Returns:
            str: The full URL for the CreateHost API endpoint
        """
        # Method 1: Direct environment variable
        api_url = os.environ.get('CREATE_HOST_API_URL')
        if api_url:
            print(f"✓ Using CREATE_HOST_API_URL from environment: {api_url}")
            return api_url
        
        # Method 2: Composite from base URL + endpoint
        base_url = os.environ.get('API_BASE_URL')
        endpoint = os.environ.get('CREATE_HOST_ENDPOINT')
        if base_url and endpoint:
            api_url = urljoin(base_url, endpoint)
            print(f"✓ Using API_BASE_URL + CREATE_HOST_ENDPOINT: {api_url}")
            return api_url
        
        # Method 3: Load from configuration file
        try:
            with open(config_path, 'r') as f:
                config = json.load(f)
                
            # Check for environment-specific configuration
            env = os.environ.get('ENVIRONMENT', 'development')
            if env in config.get('environments', {}):
                base_url = config['environments'][env]['baseUrl']
            else:
                base_url = config['api']['baseUrl']
            
            endpoint = config['api']['endpoints']['createHost']
            api_url = urljoin(base_url, endpoint)
            print(f"✓ Using configuration file ({env}): {api_url}")
            return api_url
        except (FileNotFoundError, KeyError, json.JSONDecodeError) as e:
            print(f"⚠ Warning: Could not load config file: {e}")
        
        # Method 4: Default fallback
        api_url = 'http://localhost:8080/v1/hosts/create'
        print(f"✓ Using default URL: {api_url}")
        return api_url
    
    def get_api_url(self) -> str:
        """
        Get the CreateHost API URL
        
        Returns:
            str: The full URL for the CreateHost API endpoint
        """
        return self.api_url
    
    def validate_url(self) -> bool:
        """
        Validate that the URL is properly formatted
        
        Returns:
            bool: True if URL is valid, False otherwise
        """
        try:
            result = urlparse(self.api_url)
            is_valid = all([result.scheme, result.netloc])
            if is_valid:
                print(f"✓ URL is valid: {self.api_url}")
            else:
                print(f"✗ URL is invalid: {self.api_url}")
            return is_valid
        except Exception as e:
            print(f"✗ URL validation error: {e}")
            return False
    
    def create_host(self, host_data: Dict) -> Dict:
        """
        Create a new host using the API
        
        Args:
            host_data: Dictionary containing host information
            
        Returns:
            Dict: API response
        """
        url = self.get_api_url()
        print(f"\n📡 Making request to: {url}")
        print(f"📦 Payload: {json.dumps(host_data, indent=2)}")
        
        # Note: This is a demonstration. In a real implementation,
        # you would use requests library:
        # import requests
        # response = requests.post(url, json=host_data, timeout=self.timeout)
        # return response.json()
        
        # For demonstration purposes, return a mock response
        return {
            "status": "success",
            "message": "Host would be created at this URL",
            "url": url,
            "data": host_data
        }
    
    def print_url_info(self):
        """Print detailed information about the API URL"""
        parsed = urlparse(self.api_url)
        print("\n" + "="*60)
        print("CreateHost API URL Information")
        print("="*60)
        print(f"Full URL:    {self.api_url}")
        print(f"Scheme:      {parsed.scheme}")
        print(f"Host:        {parsed.netloc}")
        print(f"Path:        {parsed.path}")
        print(f"Timeout:     {self.timeout}s")
        print("="*60 + "\n")


def main():
    """Main function demonstrating API URL access"""
    print("CreateHost API URL Access Example")
    print("-" * 60)
    
    # Initialize the API client
    client = CreateHostAPIClient()
    
    # Display URL information
    client.print_url_info()
    
    # Validate the URL
    if not client.validate_url():
        print("⚠ Warning: URL validation failed")
        sys.exit(1)
    
    # Example: Create a host
    host_data = {
        "hostname": "test-server-001",
        "ip_address": "192.168.1.100",
        "description": "Test server for integration testing",
        "tags": ["test", "integration"]
    }
    
    result = client.create_host(host_data)
    print(f"\n✓ Result: {json.dumps(result, indent=2)}")
    
    # Show how to get just the URL
    print(f"\n💡 To get just the URL in your code:")
    print(f"   url = client.get_api_url()")
    print(f"   # Returns: {client.get_api_url()}")


if __name__ == '__main__':
    main()
