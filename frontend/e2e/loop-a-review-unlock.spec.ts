import { test, expect, APIRequestContext } from '@playwright/test';

const BACKEND = 'http://localhost:5299/api/v1';
const ANALYSIS_ID = 1;

async function apiLogin(request: APIRequestContext, username: string, password: string): Promise<string> {
  const res = await request.post(`${BACKEND}/auth/login`, { data: { username, password } });
  expect(res.ok(), `login failed for ${username}: ${res.status()} ${await res.text()}`).toBeTruthy();
  const body = await res.json();
  return body.token as string;
}

async function apiGetAnalysis(request: APIRequestContext, token: string) {
  const res = await request.get(`${BACKEND}/analyses/${ANALYSIS_ID}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(res.ok(), `get analysis failed: ${res.status()} ${await res.text()}`).toBeTruthy();
  return res.json();
}

async function apiSearchAnalyses(request: APIRequestContext, token: string) {
  const res = await request.post(`${BACKEND}/search/results`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {},
    params: { pageNumber: 1, pageSize: 50 }
  });
  expect(res.ok(), `search failed: ${res.status()} ${await res.text()}`).toBeTruthy();
  const body = await res.json();
  return body.items || [];
}

test('Loop A: capture -> validate -> exception -> review -> unlock', async ({ page, request }) => {
  // --- Step 1 (UI): log in as the analyst ---
  await page.goto('/login');
  await page.locator('#username').fill('inkerman_analyst');
  await page.locator('#password').fill('inkerman_analyst_password');
  await page.getByRole('button', { name: 'Sign In' }).click();
  await page.waitForURL(/\/analysis/);

  // --- Step 2 (UI): navigate to the analysis and submit an out-of-tolerance reading ---
  await page.goto(`/analysis/analysis/${ANALYSIS_ID}`);
  await expect(page.locator('h1')).toHaveText('Analysis Execution');

  const testId = `E2E-OOT-${Date.now()}`;
  await page.locator('#testId').fill(testId);
  await page.locator('#value').fill('999');
  await page.locator('#unit').fill('mg');

  // The captured-at field is a Kendo datetimepicker, not a plain input. Try typing directly
  // into its inner input first; Kendo's masked input accepts sequential keystrokes better than
  // a single .fill(). If that doesn't produce a valid (non-disabled-submit) state, click the
  // input and use pressSequentially instead before giving up.
  const dtInput = page.locator('#capturedAtUtc input, kendo-datetimepicker input').first();
  await dtInput.click();
  const now = new Date();
  const formatted = `${String(now.getMonth() + 1).padStart(2, '0')}/${String(now.getDate()).padStart(2, '0')}/${now.getFullYear()} 12:00 PM`;
  await dtInput.pressSequentially(formatted, { delay: 30 });
  await page.keyboard.press('Escape');

  await page.getByRole('button', { name: 'Submit Reading' }).click();

  // --- Step 3 (UI): confirm the exception this out-of-tolerance reading created ---
  // The reading list should now show our test id with an invalid badge.
  const newReadingItem = page.locator('.reading-item', { hasText: testId });
  await expect(newReadingItem).toBeVisible({ timeout: 10000 });
  await expect(newReadingItem.locator('.invalid-badge')).toBeVisible();

  // Find the newest exception item (the last one in the list — this analysis may already have
  // older resolved exceptions from prior sessions, that's expected and fine).
  const exceptionItems = page.locator('.exception-item');
  const newestException = exceptionItems.last();
  await expect(newestException).toBeVisible();

  // --- Step 4 (UI): resolve the exception as the analyst ---
  await newestException.locator('kendo-dropdownlist').click();
  await page.getByRole('option', { name: 'AcceptWithComment' }).click();
  await newestException.locator('textarea').fill('E2E: accepting with comment as part of automated smoke test');
  await newestException.getByRole('button', { name: 'Resolve Exception' }).click();

  await expect(newestException.locator('.resolved-info')).toBeVisible({ timeout: 10000 });
  await expect(newestException.locator('.resolved-info')).toContainText('AcceptWithComment');

  // --- Step 5 (API setup — no UI exists anywhere in this app for completing an analysis, only
  // for capturing/reviewing/unlocking; this is a real, confirmed constraint, not a shortcut) ---
  const analystToken = await apiLogin(request, 'inkerman_analyst', 'inkerman_analyst_password');
  const beforeComplete = await apiGetAnalysis(request, analystToken);
  const completeRes = await request.patch(`${BACKEND}/analyses/${ANALYSIS_ID}/status`, {
    headers: { Authorization: `Bearer ${analystToken}` },
    data: { action: 'Complete', rowVersion: beforeComplete.rowVersion },
  });
  expect(completeRes.ok(), `complete failed: ${completeRes.status()} ${await completeRes.text()}`).toBeTruthy();
  const completeBody = await completeRes.json();
  expect(completeBody.isLocked).toBe(true);

  // --- Step 6 (UI): log in as the coordinator and unlock it through the real Exception Review screen ---
  await page.goto('/login');
  await page.locator('#username').fill('inkerman_coord');
  await page.locator('#password').fill('inkerman_coord_password');
  await page.getByRole('button', { name: 'Sign In' }).click();
  await page.waitForURL(/\/analysis/);

  await page.goto('/analysis/exception-review');
  await expect(page.locator('h1')).toHaveText('Exception Review');

  // The grid renders one row per exception; find the row for our sample and click its Unlock button.
  const row = page.locator('kendo-grid-row, tr', { hasText: 'VERIFY-SAMPLE-1' }).first();
  const unlockButton = row.getByRole('button', { name: 'Unlock result' });
  await expect(unlockButton).toBeVisible({ timeout: 10000 });
  await unlockButton.click();

  await expect(page.getByText('Unlock Result')).toBeVisible();
  await page.locator('#justification').fill('E2E: unlocking as part of automated smoke test verification');
  await page.getByRole('button', { name: 'Unlock', exact: true }).click();

  // Dialog should close on success, and (per shape.md C11/R59 pattern) no error alert shown.
  await expect(page.getByText('Unlock Result')).not.toBeVisible({ timeout: 10000 });

  // --- Final verification (API): confirm it's really unlocked, not just UI-optimistic ---
  const coordToken = await apiLogin(request, 'inkerman_coord', 'inkerman_coord_password');
  const searchResults = await apiSearchAnalyses(request, coordToken);
  const unlockedAnalysis = searchResults.find((item: any) => item.analysisId === ANALYSIS_ID);
  expect(unlockedAnalysis, `Analysis ${ANALYSIS_ID} not found in search results`).toBeDefined();
  expect(unlockedAnalysis.isLocked).toBe(false);
});
