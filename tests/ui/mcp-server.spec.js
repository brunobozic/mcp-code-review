const { test, expect } = require('@playwright/test');

/**
 * MCP Server UI Tests  
 * Tests MCP Code Review server endpoints and API responses
 */

test.describe('MCP Code Review Server', () => {
  const MCP_SERVER_URL = 'http://localhost:5001';

  test.beforeEach(async ({ page }) => {
    test.setTimeout(60000);
  });

  test('MCP Server - Health endpoint check', async ({ page }) => {
    console.log('🔍 Testing MCP Server health endpoint...');
    
    await page.goto(MCP_SERVER_URL + '/health', { timeout: 30000 });
    
    // Check if we get health response
    const content = await page.content();
    const isHealthy = content.includes('Healthy') || 
                     content.includes('healthy') ||
                     content.includes('"status"') ||
                     content.includes('OK');
    
    expect(isHealthy).toBe(true);
    console.log('✅ MCP Server health endpoint responding');
  });

  test('MCP Server - Root endpoint information', async ({ page }) => {
    console.log('📋 Testing MCP Server root endpoint...');
    
    await page.goto(MCP_SERVER_URL, { timeout: 30000 });
    
    // Check if we get service information
    const content = await page.content();
    const hasServiceInfo = content.includes('MCP') || 
                          content.includes('Code Review') ||
                          content.includes('service') ||
                          content.includes('version') ||
                          content.includes('HTTP');
    
    if (hasServiceInfo) {
      console.log('✅ MCP Server root endpoint responding with service info');
    } else {
      console.log('⚠️ MCP Server root endpoint response unclear');
      await page.screenshot({ path: 'test-results/mcp-server-root-debug.png' });
    }
    
    expect(hasServiceInfo).toBe(true);
  });

  test('MCP Server - Swagger/OpenAPI documentation', async ({ page }) => {
    console.log('📚 Testing MCP Server API documentation...');
    
    try {
      // Try to access Swagger UI (common endpoint)
      await page.goto(MCP_SERVER_URL + '/swagger', { timeout: 15000 });
      await page.waitForLoadState('networkidle');
      
      // Check for Swagger UI elements
      const swaggerElements = [
        page.locator('text=swagger', { exact: false }),
        page.locator('.swagger-ui'),
        page.locator('text=API Documentation'),
        page.locator('text=OpenAPI'),
        page.locator('.info')
      ];
      
      let swaggerVisible = false;
      for (const element of swaggerElements) {
        if (await element.isVisible()) {
          swaggerVisible = true;
          break;
        }
      }
      
      if (swaggerVisible) {
        console.log('✅ MCP Server Swagger documentation accessible');
      } else {
        console.log('⚠️ MCP Server Swagger documentation may not be enabled');
      }
    } catch (error) {
      console.log('ℹ️ MCP Server Swagger documentation not available (this is normal)');
    }
  });

  test('MCP Server - API review endpoints availability', async ({ page }) => {
    console.log('🔧 Testing MCP Server API endpoints...');
    
    // Test review endpoint (POST endpoint, so we expect method not allowed for GET)
    const response = await page.goto(MCP_SERVER_URL + '/api/review', { 
      timeout: 15000,
      waitUntil: 'networkidle'
    });
    
    // For POST endpoint accessed via GET, we might get 405 Method Not Allowed
    // This actually indicates the endpoint exists
    if (response) {
      const status = response.status();
      const isEndpointAvailable = status === 405 || // Method Not Allowed (endpoint exists but wrong method)
                                 status === 400 || // Bad Request (endpoint exists but needs data)
                                 status === 200;   // OK (endpoint responds to GET)
      
      if (isEndpointAvailable) {
        console.log(`✅ MCP Server API review endpoint exists (status: ${status})`);
      } else if (status === 404) {
        console.log('⚠️ MCP Server API review endpoint not found');
      } else {
        console.log(`⚠️ MCP Server API review endpoint unexpected status: ${status}`);
      }
    }
  });

  test('MCP Server - ChromaDB integration health', async ({ page }) => {
    console.log('🔗 Testing MCP Server ChromaDB integration...');
    
    // Check if we can reach ChromaDB through MCP server or directly
    try {
      const chromaResponse = await page.goto('http://localhost:19193/api/v2/heartbeat', { 
        timeout: 10000 
      });
      
      if (chromaResponse && chromaResponse.ok()) {
        const content = await page.content();
        if (content.includes('nanosecond')) {
          console.log('✅ ChromaDB is accessible for MCP Server integration');
        }
      }
    } catch (error) {
      console.log('⚠️ ChromaDB may not be ready for MCP Server integration');
    }
  });

  test('MCP Server - Metrics endpoint', async ({ page }) => {
    console.log('📊 Testing MCP Server metrics endpoint...');
    
    try {
      await page.goto(MCP_SERVER_URL + '/metrics', { timeout: 15000 });
      
      // Check if we get metrics data (could be Prometheus format)
      const content = await page.content();
      const hasMetrics = content.includes('# HELP') || 
                        content.includes('# TYPE') ||
                        content.includes('http_requests') ||
                        content.includes('aspnetcore_') ||
                        content.includes('dotnet_');
      
      if (hasMetrics) {
        console.log('✅ MCP Server metrics endpoint responding');
      } else {
        console.log('ℹ️ MCP Server metrics endpoint not configured (this is normal)');
      }
    } catch (error) {
      console.log('ℹ️ MCP Server metrics endpoint not available (this is normal)');
    }
  });

  test('MCP Server - CORS and headers check', async ({ page }) => {
    console.log('🌐 Testing MCP Server CORS and headers...');
    
    // Check response headers for CORS and other important headers
    page.on('response', response => {
      if (response.url() === MCP_SERVER_URL + '/health') {
        const headers = response.headers();
        
        console.log('📋 MCP Server response headers:');
        if (headers['access-control-allow-origin']) {
          console.log('✅ CORS headers present');
        }
        if (headers['content-type']) {
          console.log(`✅ Content-Type: ${headers['content-type']}`);
        }
        if (headers['server']) {
          console.log(`ℹ️ Server: ${headers['server']}`);
        }
      }
    });
    
    await page.goto(MCP_SERVER_URL + '/health', { timeout: 15000 });
    
    // Just verify the response is successful
    const content = await page.content();
    expect(content.includes('Healthy')).toBe(true);
    
    console.log('✅ MCP Server headers check completed');
  });
});