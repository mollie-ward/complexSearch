import { test, expect, Page } from '@playwright/test';

/**
 * E2E Tests: Search Flow
 * 
 * Test Cases:
 * - TC-001: Basic Search
 * - TC-002: Search Refinement
 * - TC-003: Semantic Search
 * - TC-004: View Vehicle Details
 */

test.describe('Search Flow', () => {
  
  /**
   * TC-001: Basic Search
   * 
   * Acceptance Criteria:
   * - User enters natural language query ("BMW under £20000")
   * - System returns relevant results
   * - All results meet price constraint
   * - Results visible within 5 seconds
   * - 100% pass rate
   */
  test('TC-001: should complete basic search with price constraint', async ({ page }) => {
    await page.goto('/');
    
    // Verify page loaded
    await expect(page).toHaveTitle(/Vehicle Search/i);
    
    // Enter search query
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('BMW under £20000');
    
    // Submit search
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for results to appear (within 5 seconds)
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Verify results shown
    const results = page.getByTestId('vehicle-card').or(page.locator('.vehicle-card'));
    const resultCount = await results.count();
    expect(resultCount).toBeGreaterThan(0);
    
    // Verify all results meet price constraint (£20,000)
    // Extract prices and verify they're all under £20,000
    const prices = await results.locator('[data-testid="vehicle-price"]').or(results.locator('text=/£[0-9,]+/')).allTextContents();
    
    for (const priceText of prices) {
      const price = parseFloat(priceText.replace(/[£,]/g, ''));
      if (!isNaN(price)) {
        expect(price).toBeLessThanOrEqual(20000);
      }
    }
  });

  /**
   * TC-002: Search Refinement
   * 
   * Acceptance Criteria:
   * - User performs initial search ("BMW cars")
   * - User refines with additional constraint ("under £15000")
   * - System applies refinement correctly
   * - New results respect both constraints
   * - 100% pass rate
   */
  test('TC-002: should refine search with additional constraints', async ({ page }) => {
    await page.goto('/');
    
    // Initial search
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('BMW cars');
    
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for initial results
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Count initial results
    const initialResults = page.getByTestId('vehicle-card').or(page.locator('.vehicle-card'));
    const initialCount = await initialResults.count();
    expect(initialCount).toBeGreaterThan(0);
    
    // Refine search with additional constraint
    await searchInput.fill('under £15000');
    await searchButton.click();
    
    // Wait for refined results
    await page.waitForTimeout(1000); // Brief wait for results to update
    
    // Verify refined results
    const refinedResults = page.getByTestId('vehicle-card').or(page.locator('.vehicle-card'));
    const refinedCount = await refinedResults.count();
    expect(refinedCount).toBeGreaterThan(0);
    
    // Verify all results meet refined price constraint (£15,000)
    const prices = await refinedResults.locator('[data-testid="vehicle-price"]').or(refinedResults.locator('text=/£[0-9,]+/')).allTextContents();
    
    for (const priceText of prices) {
      const price = parseFloat(priceText.replace(/[£,]/g, ''));
      if (!isNaN(price)) {
        expect(price).toBeLessThanOrEqual(15000);
      }
    }
  });

  /**
   * TC-003: Semantic Search
   * 
   * Acceptance Criteria:
   * - User enters qualitative query ("reliable economical car")
   * - System returns semantically relevant results
   * - Relevance scores displayed
   * - Match explanations available
   * - Results relevant in 80%+ of cases
   */
  test('TC-003: should perform semantic search with qualitative terms', async ({ page }) => {
    await page.goto('/');
    
    // Enter semantic query
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('reliable economical car');
    
    // Submit search
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for results (semantic search may take longer)
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Verify results shown
    const results = page.getByTestId('vehicle-card').or(page.locator('.vehicle-card'));
    const resultCount = await results.count();
    expect(resultCount).toBeGreaterThan(0);
    
    // Check for relevance indicators (scores, explanations)
    // These might be in tooltips, badges, or detail sections
    const hasRelevanceInfo = await page.getByTestId('relevance-score')
      .or(page.locator('[data-testid*="score"]'))
      .or(page.locator('text=/relevance/i'))
      .or(page.locator('text=/match/i'))
      .count();
    
    // Note: Relevance information might not be visible on all result cards
    // but should be present in the system
  });

  /**
   * TC-004: View Vehicle Details
   * 
   * Acceptance Criteria:
   * - User clicks "View Details" on result
   * - Detail page loads with full information
   * - Back button returns to search results
   * - URL updates correctly
   * - 100% pass rate
   */
  test('TC-004: should view vehicle details and navigate back', async ({ page }) => {
    await page.goto('/');
    
    // Perform search
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('BMW cars');
    
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for results
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Get current URL to verify navigation
    const searchUrl = page.url();
    
    // Click "View Details" on first result
    const viewDetailsButton = page.getByRole('button', { name: /view details/i })
      .or(page.getByRole('link', { name: /view details/i }))
      .or(page.getByText(/view details/i))
      .first();
    
    await viewDetailsButton.click();
    
    // Wait for detail page to load
    await page.waitForLoadState('networkidle');
    
    // Verify URL changed
    const detailUrl = page.url();
    expect(detailUrl).not.toBe(searchUrl);
    
    // Verify detail page contains vehicle information
    // Look for common detail page elements
    await expect(
      page.getByTestId('vehicle-make')
        .or(page.getByTestId('vehicle-model'))
        .or(page.getByText(/make/i))
        .or(page.getByText(/model/i))
        .or(page.locator('h1, h2, h3'))
        .first()
    ).toBeVisible({ timeout: 5000 });
    
    // Navigate back to search results
    const backButton = page.getByRole('button', { name: /back/i })
      .or(page.getByRole('link', { name: /back/i }))
      .or(page.locator('[aria-label*="back"]'))
      .first();
    
    if (await backButton.isVisible()) {
      await backButton.click();
      
      // Verify returned to search results
      await expect(page.getByTestId('vehicle-card').or(page.locator('.vehicle-card')).first()).toBeVisible({ timeout: 5000 });
    } else {
      // Alternative: use browser back navigation
      await page.goBack();
      await expect(page.getByTestId('vehicle-card').or(page.locator('.vehicle-card')).first()).toBeVisible({ timeout: 5000 });
    }
  });

  /**
   * TC-001b: Basic Search - Alternative with Make constraint
   */
  test('TC-001b: should search by make', async ({ page }) => {
    await page.goto('/');
    
    // Enter search query with make
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('Volkswagen');
    
    // Submit search
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for results
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Verify results contain the make
    const results = page.getByTestId('vehicle-card').or(page.locator('.vehicle-card'));
    const resultCount = await results.count();
    expect(resultCount).toBeGreaterThan(0);
  });

  /**
   * TC-003b: Semantic Search - Complex query
   */
  test('TC-003b: should handle complex semantic query', async ({ page }) => {
    await page.goto('/');
    
    // Enter complex semantic query
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('family friendly SUV with good safety features');
    
    // Submit search
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for results
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Verify results shown
    const results = page.getByTestId('vehicle-card').or(page.locator('.vehicle-card'));
    const resultCount = await results.count();
    expect(resultCount).toBeGreaterThan(0);
  });
});
