// CodeBuild supplies this constant through Angular's --define option, never a source file.
declare const GOOGLE_MAPS_API_KEY_DEV: string | undefined;

export const googleMapsApiKey =
  typeof GOOGLE_MAPS_API_KEY_DEV === 'undefined' ? '' : GOOGLE_MAPS_API_KEY_DEV;
