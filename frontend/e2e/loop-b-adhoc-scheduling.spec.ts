import { test, expect } from '@playwright/test';

test('Loop B: create an ad-hoc schedule through the UI', async ({ page }) => {
  // Step 1: log in as coordinator (only coordinators can create schedules)
  await page.goto('/login');
  await page.locator('#username').fill('inkerman_coord');
  await page.locator('#password').fill('inkerman_coord_password');
  await page.getByRole('button', { name: 'Sign In' }).click();
  await page.waitForURL(/\/analysis/);

  // Step 2: navigate to Schedules and click Create Schedule
  await page.goto('/analysis/schedules');
  await expect(page.locator('h1')).toHaveText('Schedules');
  await page.getByRole('link', { name: 'Create Schedule' }).click();
  await expect(page.locator('h1')).toHaveText('Create Schedule');

  // Step 3: fill the form with real values
  const scheduleName = `E2E Ad-hoc Schedule ${Date.now()}`;
  await page.locator('#name').fill(scheduleName);
  await page.locator('#site').click();
  await page.getByRole('option', { name: 'Inkerman' }).click();
  await page.locator('#shiftPattern').click();
  await page.getByRole('option', { name: '3x8' }).click();
  await page.locator('#analysisType').fill('E2E Ad-hoc Analysis');

  // Step 4: submit and confirm it lands back on the schedules list showing the new row
  await page.getByRole('button', { name: 'Save Schedule' }).click();
  await page.waitForURL(/\/analysis\/schedules$/, { timeout: 10000 });
  await expect(page.getByText(scheduleName)).toBeVisible({ timeout: 10000 });
});
