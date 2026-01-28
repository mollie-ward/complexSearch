import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

/**
 * k6 Load Testing Script for Vehicle Search API
 * 
 * Load Profile:
 * - Stage 1: Ramp 0→50 users (1 min)
 * - Stage 2: Sustain 50 users (3 min)
 * - Stage 3: Ramp 50→100 users (1 min)
 * - Stage 4: Sustain 100 users (2 min)
 * - Stage 5: Ramp 100→0 users (1 min)
 * 
 * Thresholds:
 * - P95 response time <3 seconds
 * - Error rate <1%
 * - Successful search results >95%
 * 
 * Run: k6 run tests/load/search-load.js
 */

// Configuration
const BASE_URL = __ENV.API_URL || 'http://localhost:5001';
const API_ENDPOINT = `${BASE_URL}/api/search`;

// Custom metrics
const errorRate = new Rate('errors');
const successfulSearches = new Rate('successful_searches');

// Load test configuration
export const options = {
  stages: [
    { duration: '1m', target: 50 },   // Ramp up to 50 users
    { duration: '3m', target: 50 },   // Stay at 50 users
    { duration: '1m', target: 100 },  // Ramp up to 100 users
    { duration: '2m', target: 100 },  // Stay at 100 users
    { duration: '1m', target: 0 },    // Ramp down to 0 users
  ],
  thresholds: {
    'http_req_duration': ['p(95)<3000'], // 95% of requests should be below 3s
    'errors': ['rate<0.01'],             // Error rate should be less than 1%
    'successful_searches': ['rate>0.95'], // 95%+ searches should return results
    'http_req_failed': ['rate<0.01'],    // Less than 1% failed requests
  },
};

// Sample search queries for realistic load testing
const searchQueries = [
  'BMW under £20000',
  'reliable economical car',
  'Volkswagen Golf',
  'family SUV with good safety features',
  'Honda Civic under £15000',
  'Mercedes sedan',
  'low mileage Toyota',
  'sporty Audi',
  'cheap Ford Focus',
  'hybrid electric car under £25000',
  'diesel BMW 3 Series',
  'automatic transmission Volkswagen',
  'red sports car',
  'estate car for large family',
  'fuel efficient hatchback',
];

// Helper function to get random query
function getRandomQuery() {
  return searchQueries[Math.floor(Math.random() * searchQueries.length)];
}

// Main test scenario
export default function () {
  const query = getRandomQuery();
  
  const payload = JSON.stringify({
    query: query,
    sessionId: `load-test-${__VU}-${__ITER}`, // Unique session per VU and iteration
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    tags: { name: 'SearchRequest' },
  };

  // Execute search request
  const response = http.post(API_ENDPOINT, payload, params);

  // Check response status
  const success = check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 3s': (r) => r.timings.duration < 3000,
    'response has results': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.results && body.results.length > 0;
      } catch {
        return false;
      }
    },
  });

  // Track metrics
  errorRate.add(!success);
  
  if (response.status === 200) {
    try {
      const body = JSON.parse(response.body);
      successfulSearches.add(body.results && body.results.length > 0);
    } catch {
      successfulSearches.add(false);
    }
  } else {
    successfulSearches.add(false);
  }

  // Think time between requests (1-3 seconds)
  sleep(Math.random() * 2 + 1);
}

// Setup function (runs once per VU at the start)
export function setup() {
  console.log(`Starting load test against ${BASE_URL}`);
  console.log('Load profile: 0→50→100 users over 8 minutes');
  
  // Verify API is reachable
  const healthCheck = http.get(`${BASE_URL}/health`);
  if (healthCheck.status !== 200) {
    console.warn('Warning: Health check failed. API might not be ready.');
  }
  
  return { startTime: new Date().toISOString() };
}

// Teardown function (runs once at the end)
export function teardown(data) {
  console.log(`Load test completed. Started at ${data.startTime}`);
}

/**
 * Alternative Test Scenarios
 */

// Spike test: sudden load increase
export const spikeOptions = {
  stages: [
    { duration: '30s', target: 10 },   // Warm up
    { duration: '10s', target: 200 },  // Spike!
    { duration: '2m', target: 200 },   // Stay at spike
    { duration: '30s', target: 10 },   // Scale down
  ],
};

// Stress test: gradually increase to breaking point
export const stressOptions = {
  stages: [
    { duration: '2m', target: 50 },
    { duration: '2m', target: 100 },
    { duration: '2m', target: 200 },
    { duration: '2m', target: 300 },
    { duration: '2m', target: 400 },
    { duration: '5m', target: 0 },
  ],
};

// Soak test: sustained load over long period
export const soakOptions = {
  stages: [
    { duration: '2m', target: 50 },    // Ramp up
    { duration: '60m', target: 50 },   // Stay for 1 hour
    { duration: '2m', target: 0 },     // Ramp down
  ],
};
