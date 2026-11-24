import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ErrorTextBehaviour } from '../../../../../../behaviours/errors/error-text.behaviour';
import { isUploadable } from '../../../../../../behaviours/files/uploadable-file';
import { ApiErrorCode } from '../../../../../../entities/errors/api-error-codes';
import {
  IMAGE_UPLOAD_TYPES,
  MAX_IMAGE_UPLOAD_MEGABYTES,
  MEGABYTE,
} from '../../../../../../entities/shared/image-upload-types';
import { IconButton } from '../../../../../shared/ui/buttons/icon-button/icon-button';
import { SectionTitle } from '../../../../../shared/ui/titles/section-title/section-title';
import { FileDropZone } from '../../../../../shared/ui/uploads/file-drop-zone/file-drop-zone';

/** An icon of the grid: the value saved for the service and its image. */
export interface ServicesImagesIcon<K extends string = string> {
  value: K;
  url: string;
}

/** The image of a service: one of the icons given, or an image from the computer. */
@Component({
  selector: 'app-services-images',
  imports: [FileDropZone, IconButton, SectionTitle, TranslatePipe],
  templateUrl: './services-images.html',
  styleUrl: './services-images.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesImages<K extends string = string> {
  /** The icons the service can have, in the order of the grid. */
  readonly icons = input<readonly ServicesImagesIcon<K>[]>([]);
  readonly chosenIcon = input<K | null>(null);
  readonly imagePreviewUrl = input<string | null>(null);
  /** Only to look at: icons and upload are off. */
  readonly isReadonly = input(false);
  readonly error = input<string | null>();
  /** The X on the image, for who listens to `imageRemoveRequest`. */
  readonly isImageRemovable = input(false);
  readonly iconChosen = output<K>();
  readonly imageChosen = output<File>();
  readonly imageRemoveRequest = output<void>();

  protected readonly imagesIcon = ['/assets/images/GeneralIcon_Images.svg'] as const;
  protected readonly addImageIcon = ['/assets/images/GeneralIcon_AddImage.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly imageTypes = IMAGE_UPLOAD_TYPES.join(',');
  protected readonly maxImageMegabytes = MAX_IMAGE_UPLOAD_MEGABYTES;
  /** The errors of a refused image last until something else is chosen or the panel is locked. */
  protected readonly isImageTooLarge = linkedSignal({
    source: () => [this.isReadonly(), this.chosenIcon(), this.imagePreviewUrl()],
    computation: () => false,
  });
  /** The backend's answer for an image it would refuse for another reason, e.g. an empty file. */
  protected readonly invalidImageText = linkedSignal<unknown, string | null>({
    source: () => [this.isReadonly(), this.chosenIcon(), this.imagePreviewUrl()],
    computation: () => null,
  });
  private readonly errorText = inject(ErrorTextBehaviour);

  protected chooseImage(files: readonly File[]): void {
    const [image] = files;
    if (!image) {
      return;
    }
    this.isImageTooLarge.set(image.size > MAX_IMAGE_UPLOAD_MEGABYTES * MEGABYTE);
    const isInvalid =
      !this.isImageTooLarge() &&
      !isUploadable(image, IMAGE_UPLOAD_TYPES, MAX_IMAGE_UPLOAD_MEGABYTES);
    this.invalidImageText.set(
      isInvalid
        ? this.errorText.instant(ApiErrorCode.CatalogInvalidImage, {
            maxBytes: MAX_IMAGE_UPLOAD_MEGABYTES * MEGABYTE,
          })
        : null,
    );
    if (!this.isImageTooLarge() && !isInvalid) {
      this.imageChosen.emit(image);
    }
  }
}
