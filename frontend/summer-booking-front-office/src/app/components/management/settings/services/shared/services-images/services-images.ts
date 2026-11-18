import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import {
  IMAGE_UPLOAD_TYPES,
  MAX_IMAGE_UPLOAD_MEGABYTES,
  MEGABYTE,
} from '../../../../../../entities/shared/image-upload-types';
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
  imports: [FileDropZone, SectionTitle, TranslatePipe],
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
  readonly iconChosen = output<K>();
  readonly imageChosen = output<File>();

  protected readonly imagesIcon = ['/assets/images/GeneralIcon_Images.svg'] as const;
  protected readonly addImageIcon = ['/assets/images/GeneralIcon_AddImage.svg'] as const;
  protected readonly imageTypes = IMAGE_UPLOAD_TYPES.join(',');
  protected readonly maxImageMegabytes = MAX_IMAGE_UPLOAD_MEGABYTES;
  protected readonly isImageTooLarge = signal(false);

  protected chooseImage(files: readonly File[]): void {
    const [image] = files;
    if (!image) {
      return;
    }
    this.isImageTooLarge.set(image.size > MAX_IMAGE_UPLOAD_MEGABYTES * MEGABYTE);
    if (!this.isImageTooLarge()) {
      this.imageChosen.emit(image);
    }
  }
}
