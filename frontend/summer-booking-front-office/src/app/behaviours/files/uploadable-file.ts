import { MEGABYTE } from '../../entities/shared/image-upload-types';

/** As the backend checks every upload: an allowed type, not empty, at most `maxMegabytes`. */
export function isUploadable(file: File, types: readonly string[], maxMegabytes: number): boolean {
  return types.includes(file.type) && file.size > 0 && file.size <= maxMegabytes * MEGABYTE;
}
