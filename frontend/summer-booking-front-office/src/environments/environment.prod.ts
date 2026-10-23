import { googleMapsApiKey } from './google-maps-api-key';

// Production: real API only; mock code is replaced at build time (angular.json, fileReplacements).
export const environment = {
  production: true,
  apiBaseUrl: '/api',
  authApp: 'hub',
  stripeClientId: '',
  stripeConnectReturnUrl: '',
  googleMapsApiKey,
  googleMapsMapId: '',
} as const;
