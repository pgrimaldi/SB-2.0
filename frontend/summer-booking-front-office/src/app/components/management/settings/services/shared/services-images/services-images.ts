import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
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

  protected chooseImage(files: readonly File[]): void {
    const [image] = files;
    if (image) {
      this.imageChosen.emit(image);
    }
  }
}
