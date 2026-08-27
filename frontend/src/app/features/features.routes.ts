import { Routes } from '@angular/router';
import { AuthenticatedLayout } from '../shared/layouts/authenticated-layout/authenticated.layout';
import { analysisExecutionRoutes } from './analysis-execution/analysis-execution.routes';
import { templatesRoutes } from './templates/templates.routes';
import { schedulingRoutes } from './scheduling/scheduling.routes';
import { exceptionReviewRoutes } from './exception-review/exception-review.routes';
import { historySearchRoutes } from './history-search/history-search.routes';
import { sampleTransferRoutes } from './sample-transfer/sample-transfer.routes';
import { calibrationCurvesRoutes } from './calibration-curves/calibration-curves.routes';
import { auditTrailRoutes } from './audit-trail/audit-trail.routes';
import { integrationMonitoringRoutes } from './integration-monitoring/integration-monitoring.routes';
import { resultComparisonRoutes } from './result-comparison/result-comparison.routes';

export const featuresRoutes: Routes = [
  {
    path: '',
    component: AuthenticatedLayout,
    children: [
      { path: '', redirectTo: 'work-queue', pathMatch: 'full' },
      ...analysisExecutionRoutes,
      ...templatesRoutes,
      ...schedulingRoutes,
      ...exceptionReviewRoutes,
      ...historySearchRoutes,
      ...sampleTransferRoutes,
      ...calibrationCurvesRoutes,
      ...auditTrailRoutes,
      ...integrationMonitoringRoutes,
      ...resultComparisonRoutes,
    ],
  },
];
