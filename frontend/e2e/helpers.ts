import { APIRequestContext } from '@playwright/test';

const BACKEND_URL = 'http://localhost:5299/api/v1';

interface SearchResult {
  analysisId: number;
  sampleId: number;
  status: string;
  [key: string]: any;
}

export async function getAuthToken(request: APIRequestContext, username: string, password: string): Promise<string> {
  const response = await request.post(`${BACKEND_URL}/auth/login`, {
    data: { username, password }
  });

  if (!response.ok()) {
    throw new Error(`Login failed: ${response.status()}`);
  }

  const data = await response.json() as any;
  return data.token || data.accessToken;
}

export async function searchAnalyses(
  request: APIRequestContext,
  token: string,
  status?: string
): Promise<SearchResult[]> {
  const response = await request.post(`${BACKEND_URL}/search/results`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {},
    params: { pageNumber: 1, pageSize: 50 }
  });

  if (!response.ok()) {
    throw new Error(`Search failed: ${response.status()}`);
  }

  const data = await response.json() as any;
  const items = data.items || [];

  if (status) {
    return items.filter((item: SearchResult) => item.status === status);
  }

  return items;
}

export async function getAnalysisDetail(
  request: APIRequestContext,
  token: string,
  analysisId: number
): Promise<any> {
  const response = await request.get(`${BACKEND_URL}/analyses/${analysisId}`, {
    headers: { Authorization: `Bearer ${token}` }
  });

  if (!response.ok()) {
    throw new Error(`Get analysis failed: ${response.status()}`);
  }

  return response.json();
}

export async function findAnalysisInStatus(
  request: APIRequestContext,
  token: string,
  desiredStatus: string
): Promise<SearchResult | null> {
  const analyses = await searchAnalyses(request, token, desiredStatus);
  return analyses.length > 0 ? analyses[0] : null;
}

export async function createSchedule(
  request: APIRequestContext,
  token: string,
  name: string,
  site: string,
  analysisType: string,
  shiftPattern: string = 'Day'
): Promise<any> {
  const response = await request.post(`${BACKEND_URL}/schedules`, {
    headers: { Authorization: `Bearer ${token}` },
    data: {
      name,
      site,
      analysisType,
      shiftPattern,
      recurrencePattern: 'Once',
      exclusionRules: [],
      assignedToUserId: null
    }
  });

  if (!response.ok()) {
    throw new Error(`Create schedule failed: ${response.status()}`);
  }

  return response.json();
}
