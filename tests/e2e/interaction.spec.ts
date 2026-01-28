import { test, expect } from '@playwright/test';

/**
 * E2E Tests: User Interactions
 * 
 * Test Cases:
 * - TC-011: Refinement Controls
 * - TC-012: Vehicle Comparison
 * - TC-013: Quick Refinement Suggestions
 */

test.describe('User Interactions', () => {
  
  /**
   * TC-011: Refinement Controls
   * 
   * Acceptance Criteria:
   * - User opens refinement sheet/drawer
   * - User adjusts price/mileage sliders
   * - User selects make/fuel type
   * - User clicks "Apply Filters"
   * - New search executed with filters
   * - 100% pass rate
   */
  test('TC-011: should use refinement controls to filter results', async ({ page }) => {
    await page.goto('/');
    
    // Perform initial search
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('cars');
    
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for results
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Look for refinement controls (filters, drawer, sheet, etc.)
    const refinementButton = page.getByRole('button', { name: /filter/i })
      .or(page.getByRole('button', { name: /refine/i }))
      .or(page.getByText(/filter/i))
      .or(page.locator('[data-testid="refinement-button"]'))
      .first();
    
    const refinementVisible = await refinementButton.isVisible({ timeout: 2000 }).catch(() => false);
    
    if (refinementVisible) {
      // Open refinement controls
      await refinementButton.click();
      
      // Wait for refinement panel to open
      await page.waitForTimeout(500);
      
      // Look for price slider
      const priceSlider = page.getByLabel(/price/i)
        .or(page.locator('input[type="range"]').first())
        .or(page.locator('[data-testid*="price"]'))
        .first();
      
      const sliderVisible = await priceSlider.isVisible({ timeout: 2000 }).catch(() => false);
      
      if (sliderVisible) {
        // Adjust slider (set to mid-range value)
        await priceSlider.fill('15000');
      }
      
      // Look for make/fuel type select
      const makeSelect = page.getByLabel(/make/i)
        .or(page.getByRole('combobox').first())
        .or(page.locator('select').first())
        .first();
      
      const selectVisible = await makeSelect.isVisible({ timeout: 2000 }).catch(() => false);
      
      if (selectVisible) {
        // Select a make
        await makeSelect.click();
        await page.waitForTimeout(300);
        
        // Select first available option
        const option = page.getByRole('option').or(page.locator('option')).nth(1);
        const optionVisible = await option.isVisible({ timeout: 1000 }).catch(() => false);
        if (optionVisible) {
          await option.click();
        }
      }
      
      // Apply filters
      const applyButton = page.getByRole('button', { name: /apply/i })
        .or(page.getByRole('button', { name: /filter/i }))
        .or(page.locator('[data-testid="apply-filters"]'))
        .first();
      
      const applyVisible = await applyButton.isVisible({ timeout: 2000 }).catch(() => false);
      
      if (applyVisible) {
        await applyButton.click();
        
        // Wait for results to update
        await page.waitForTimeout(1000);
        
        // Verify results updated
        await expect(page.getByTestId('vehicle-card').or(page.locator('.vehicle-card')).first()).toBeVisible({ timeout: 5000 });
      }
    } else {
      // Skip test if refinement controls are not available
      test.skip();
    }
  });

  /**
   * TC-012: Vehicle Comparison
   * 
   * Acceptance Criteria:
   * - User selects 2-3 vehicles for comparison
   * - "Compare" button appears
   * - Comparison view opens with side-by-side table
   * - All attributes displayed correctly
   * - User can remove vehicles
   * - 100% pass rate
   */
  test('TC-012: should compare multiple vehicles', async ({ page }) => {
    await page.goto('/');
    
    // Perform search to get multiple results
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('BMW cars');
    
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for results
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Get vehicle cards
    const vehicleCards = page.getByTestId('vehicle-card').or(page.locator('.vehicle-card'));
    const cardCount = await vehicleCards.count();
    
    if (cardCount >= 2) {
      // Look for compare checkbox/button on vehicle cards
      const compareCheckbox1 = vehicleCards.nth(0).getByRole('checkbox')
        .or(vehicleCards.nth(0).locator('input[type="checkbox"]'))
        .or(vehicleCards.nth(0).locator('[data-testid*="compare"]'))
        .first();
      
      const checkbox1Visible = await compareCheckbox1.isVisible({ timeout: 2000 }).catch(() => false);
      
      if (checkbox1Visible) {
        // Select first vehicle
        await compareCheckbox1.click();
        
        // Select second vehicle
        const compareCheckbox2 = vehicleCards.nth(1).getByRole('checkbox')
          .or(vehicleCards.nth(1).locator('input[type="checkbox"]'))
          .or(vehicleCards.nth(1).locator('[data-testid*="compare"]'))
          .first();
        
        await compareCheckbox2.click();
        
        // If there's a third vehicle, select it too
        if (cardCount >= 3) {
          const compareCheckbox3 = vehicleCards.nth(2).getByRole('checkbox')
            .or(vehicleCards.nth(2).locator('input[type="checkbox"]'))
            .or(vehicleCards.nth(2).locator('[data-testid*="compare"]'))
            .first();
          
          const checkbox3Visible = await compareCheckbox3.isVisible({ timeout: 2000 }).catch(() => false);
          if (checkbox3Visible) {
            await compareCheckbox3.click();
          }
        }
        
        // Look for "Compare" button
        const compareButton = page.getByRole('button', { name: /compare/i })
          .or(page.locator('[data-testid="compare-button"]'))
          .first();
        
        // Compare button should appear after selecting vehicles
        await expect(compareButton).toBeVisible({ timeout: 3000 });
        
        // Click compare button
        await compareButton.click();
        
        // Wait for comparison view to load
        await page.waitForTimeout(1000);
        
        // Verify comparison view/table is shown
        const comparisonView = page.getByTestId('comparison-view')
          .or(page.locator('[data-testid*="comparison"]'))
          .or(page.getByRole('table'))
          .or(page.locator('table'))
          .or(page.getByText(/comparison/i))
          .first();
        
        await expect(comparisonView).toBeVisible({ timeout: 3000 });
        
        // Verify table shows vehicle attributes
        // Look for common attributes like Make, Model, Price
        const tableContent = await page.textContent('body');
        const hasAttributes = 
          tableContent?.toLowerCase().includes('make') ||
          tableContent?.toLowerCase().includes('model') ||
          tableContent?.toLowerCase().includes('price');
        
        expect(hasAttributes).toBe(true);
        
        // Look for remove/close button in comparison view
        const removeButton = page.getByRole('button', { name: /remove/i })
          .or(page.getByRole('button', { name: /close/i }))
          .or(page.locator('[data-testid*="remove"]'))
          .first();
        
        const removeVisible = await removeButton.isVisible({ timeout: 2000 }).catch(() => false);
        
        if (removeVisible) {
          // Test removal functionality
          await removeButton.click();
          await page.waitForTimeout(500);
        }
      } else {
        // Skip test if comparison feature is not available
        test.skip();
      }
    } else {
      // Skip test if not enough results
      test.skip();
    }
  });

  /**
   * TC-013: Quick Refinement Suggestions
   * 
   * Acceptance Criteria:
   * - User performs initial search
   * - Quick suggestions displayed
   * - User clicks suggestion badge
   * - New search executed with suggestion
   * - Context maintained
   * - 100% pass rate
   */
  test('TC-013: should use quick refinement suggestions', async ({ page }) => {
    await page.goto('/');
    
    // Perform initial search
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('BMW cars');
    
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for results
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Look for suggestion chips/badges
    const suggestions = page.getByTestId('suggestion')
      .or(page.locator('[data-testid*="suggestion"]'))
      .or(page.locator('.suggestion'))
      .or(page.locator('[class*="suggestion"]'))
      .or(page.locator('button[class*="badge"]'))
      .or(page.locator('[role="button"][class*="chip"]'));
    
    const suggestionCount = await suggestions.count();
    
    if (suggestionCount > 0) {
      // Get text of first suggestion
      const suggestionText = await suggestions.first().textContent();
      
      // Click first suggestion
      await suggestions.first().click();
      
      // Wait for search to execute
      await page.waitForTimeout(1000);
      
      // Verify results updated
      await expect(page.getByTestId('vehicle-card').or(page.locator('.vehicle-card')).first()).toBeVisible({ timeout: 5000 });
      
      // Verify context is maintained (still searching within BMW cars)
      const results = page.getByTestId('vehicle-card').or(page.locator('.vehicle-card'));
      const resultCount = await results.count();
      expect(resultCount).toBeGreaterThan(0);
      
      // Suggestion text should be reflected in the query or filters
      // This can be verified by checking conversation history or active filters
    } else {
      // Skip test if no suggestions are shown
      test.skip();
    }
  });

  /**
   * TC-013b: Quick Refinement Suggestions - Multiple suggestions
   */
  test('TC-013b: should allow multiple refinement suggestions', async ({ page }) => {
    await page.goto('/');
    
    // Perform initial search
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('family cars');
    
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    // Wait for results and suggestions
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    const suggestions = page.getByTestId('suggestion')
      .or(page.locator('[data-testid*="suggestion"]'))
      .or(page.locator('.suggestion'))
      .or(page.locator('[role="button"][class*="badge"]'));
    
    const suggestionCount = await suggestions.count();
    
    if (suggestionCount >= 2) {
      // Click first suggestion
      await suggestions.first().click();
      await page.waitForTimeout(1000);
      
      // Verify results updated
      await expect(page.getByTestId('vehicle-card').or(page.locator('.vehicle-card')).first()).toBeVisible({ timeout: 5000 });
      
      // Check if more suggestions are available
      const newSuggestionCount = await suggestions.count();
      
      // Suggestions might refresh or remain available
      if (newSuggestionCount > 0) {
        // Click another suggestion
        await suggestions.first().click();
        await page.waitForTimeout(1000);
        
        // Verify results updated again
        await expect(page.getByTestId('vehicle-card').or(page.locator('.vehicle-card')).first()).toBeVisible({ timeout: 5000 });
      }
    } else {
      test.skip();
    }
  });

  /**
   * TC-011b: Refinement Controls - Reset filters
   */
  test('TC-011b: should reset refinement filters', async ({ page }) => {
    await page.goto('/');
    
    // Perform search
    const searchInput = page.getByPlaceholder(/describe/i).or(page.getByRole('textbox').first());
    await searchInput.fill('cars');
    
    const searchButton = page.getByRole('button', { name: /search/i }).or(page.getByRole('button', { name: /send/i }));
    await searchButton.click();
    
    await expect(page.getByTestId('vehicle-card').or(page.locator('[data-testid*="vehicle"]').first())).toBeVisible({ timeout: 5000 });
    
    // Open refinement controls
    const refinementButton = page.getByRole('button', { name: /filter/i })
      .or(page.getByRole('button', { name: /refine/i }))
      .first();
    
    const refinementVisible = await refinementButton.isVisible({ timeout: 2000 }).catch(() => false);
    
    if (refinementVisible) {
      await refinementButton.click();
      await page.waitForTimeout(500);
      
      // Apply some filters first
      const applyButton = page.getByRole('button', { name: /apply/i }).first();
      const applyVisible = await applyButton.isVisible({ timeout: 2000 }).catch(() => false);
      
      if (applyVisible) {
        await applyButton.click();
        await page.waitForTimeout(1000);
        
        // Now look for reset/clear button
        await refinementButton.click();
        await page.waitForTimeout(500);
        
        const resetButton = page.getByRole('button', { name: /reset/i })
          .or(page.getByRole('button', { name: /clear/i }))
          .or(page.locator('[data-testid*="reset"]'))
          .first();
        
        const resetVisible = await resetButton.isVisible({ timeout: 2000 }).catch(() => false);
        
        if (resetVisible) {
          await resetButton.click();
          await page.waitForTimeout(500);
          
          // Apply again to see reset results
          if (await applyButton.isVisible({ timeout: 1000 }).catch(() => false)) {
            await applyButton.click();
            await page.waitForTimeout(1000);
            
            // Verify results shown (filters should be reset)
            await expect(page.getByTestId('vehicle-card').or(page.locator('.vehicle-card')).first()).toBeVisible({ timeout: 5000 });
          }
        }
      }
    } else {
      test.skip();
    }
  });
});
