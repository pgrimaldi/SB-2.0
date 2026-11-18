import { UploadContentType } from '../enums/upload-content-type';

export const MEGABYTE = 1024 * 1024;
export const MAX_IMAGE_UPLOAD_MEGABYTES = 5;

export const IMAGE_UPLOAD_TYPES: readonly string[] = [
  UploadContentType.Png,
  UploadContentType.Jpeg,
  UploadContentType.Webp,
];
