import { checkHealth } from '@/lib/api/client'

describe('API Client', () => {
  describe('checkHealth', () => {
    it('returns true when health endpoint is accessible', async () => {
      // Mock fetch for this test
      global.fetch = jest.fn(() =>
        Promise.resolve({
          ok: true,
        })
      ) as jest.Mock

      const result = await checkHealth()
      expect(result).toBe(true)
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining('/api/health'))
    })

    it('returns false when health endpoint fails', async () => {
      global.fetch = jest.fn(() =>
        Promise.resolve({
          ok: false,
        })
      ) as jest.Mock

      const result = await checkHealth()
      expect(result).toBe(false)
    })

    it('returns false when fetch throws an error', async () => {
      global.fetch = jest.fn(() =>
        Promise.reject(new Error('Network error'))
      ) as jest.Mock

      const consoleSpy = jest.spyOn(console, 'error').mockImplementation()
      const result = await checkHealth()
      expect(result).toBe(false)
      expect(consoleSpy).toHaveBeenCalledWith('Health check failed:', expect.any(Error))
      consoleSpy.mockRestore()
    })
  })
})
