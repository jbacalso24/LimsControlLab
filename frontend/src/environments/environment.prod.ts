import { Environment } from './environment.model';

export const environment: Environment = {
  production: true,
  // Render assigns this once the API service is created; confirm the exact host
  // in the Render dashboard and update if it differs.
  limsControlLabApiUrl: 'https://lims-api.onrender.com/api/v1',
};
