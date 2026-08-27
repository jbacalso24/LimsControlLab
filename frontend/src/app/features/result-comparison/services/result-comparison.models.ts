/** Request body for POST /api/v1/results/comparison. All fields optional filters. */
export interface ResultComparisonRequest {
  templateName?: string;
  testId?: number;
  sampleIdentifier?: string;
  fromUtc?: string;
  toUtc?: string;
}

export interface ResultComparisonPoint {
  analysisId: number | string;
  sampleId: number | string;
  sampleIdentifier: string;
  templateName: string;
  testId: number | string | null;
  value: number;
  unit: string | null;
  capturedAtUtc: string;
  validationResult: string | null;
  calibratedValue: number | null;
}

export interface ResultComparisonResponse {
  unit: string | null;
  toleranceMin: number | null;
  toleranceMax: number | null;
  totalPoints: number;
  points: ResultComparisonPoint[];
}
