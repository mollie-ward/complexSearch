import { client } from './generated/client.gen'

// Configure the API client
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001'

client.setConfig({
  baseUrl: API_BASE_URL,
})

// Export the configured client
export { client }

// Health check function
export async function checkHealth() {
  try {
    const response = await fetch(`${API_BASE_URL}/api/health`)
    return response.ok
  } catch (error) {
    console.error('Health check failed:', error)
    return false
  }
}
