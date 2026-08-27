import { Environment } from './environment.model';

export const environment: Environment = {
  production: true,
  // Azure App Service hosting the LIMS API.
  limsControlLabApiUrl: 'https://lims-controllab-api.azurewebsites.net/api/v1',
};
