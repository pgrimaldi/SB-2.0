import { CdkConnectedOverlay, CdkOverlayOrigin, ConnectedPosition } from '@angular/cdk/overlay';
import { DOCUMENT } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { fileLinkOf } from '../../../../../behaviours/files/file-link';
import { isUploadable } from '../../../../../behaviours/files/uploadable-file';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { MapBackgroundImage } from '../../../../../entities/enums/map-background-image';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { MapSettings } from '../../../../../entities/settings/map-configuration/map-settings';
import {
  IMAGE_UPLOAD_TYPES,
  MAX_IMAGE_UPLOAD_MEGABYTES,
  MEGABYTE,
} from '../../../../../entities/shared/image-upload-types';
import { MapService } from '../../../../../services/api/catalog/map.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { ConfirmPopup } from '../../../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { Spinner } from '../../../../shared/ui/spinners/spinner/spinner';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { FileDropZone } from '../../../../shared/ui/uploads/file-drop-zone/file-drop-zone';
import { VersionConflictPopup } from '../../../version-conflict-popup/version-conflict-popup';

const BACKGROUND_SLOTS = [
  MapBackgroundImage.BackgroundBeach,
  MapBackgroundImage.BackgroundBoardwalk,
  null,
  null,
] as const;

/** The backend's answer for an image it refuses, given before sending it. */
const INVALID_IMAGE: ApiProblem = {
  status: HttpStatusCode.UnprocessableEntity,
  code: ApiErrorCode.MapInvalidImage,
  args: { maxBytes: MAX_IMAGE_UPLOAD_MEGABYTES * MEGABYTE },
};

const PREVIEW_POSITIONS: ConnectedPosition[] = [
  { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: 8 },
  { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -8 },
  { originX: 'end', originY: 'bottom', overlayX: 'end', overlayY: 'top', offsetY: 8 },
  { originX: 'end', originY: 'top', overlayX: 'end', overlayY: 'bottom', offsetY: -8 },
];

@Component({
  selector: 'app-map-background-panel',
  imports: [
    Button,
    CdkConnectedOverlay,
    CdkOverlayOrigin,
    ConfirmPopup,
    FileDropZone,
    IconButton,
    SectionTitle,
    Spinner,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './map-background-panel.html',
  styleUrl: './map-background-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapBackgroundPanel implements UnsavedChanges, OnDestroy {
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly mapService = inject(MapService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly origin = inject(DOCUMENT).location.origin;

  readonly settings = input<MapSettings | null>(null);
  readonly loadError = input<ApiProblem | null>(null);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly slots = BACKGROUND_SLOTS.map(
    (name) => name && `/assets/images/MapConfiguration_${name}.png`,
  );
  protected readonly previewPositions = PREVIEW_POSITIONS;
  protected readonly titleIcon = ['/assets/images/MapConfiguration_Palette.svg'] as const;
  protected readonly chosenIcon = '/assets/images/MapConfiguration_Chosen.svg';
  protected readonly uploadIcon = ['/assets/images/GeneralIcon_AddImage.svg'] as const;
  protected readonly imageIcon = ['/assets/images/GeneralIcon_Images.svg'] as const;
  protected readonly deleteIcon = ['/assets/images/MapConfiguration_DeleteRed.svg'] as const;
  protected readonly popupIcons = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly maxImageMegabytes = MAX_IMAGE_UPLOAD_MEGABYTES;
  protected readonly imageTypes = IMAGE_UPLOAD_TYPES.join(',');
  protected readonly isEditing = signal(false);
  protected readonly isBusy = signal(false);
  protected readonly isSaving = signal(false);
  readonly isCallRunning = this.isBusy.asReadonly();
  protected readonly chosenSlot = signal<number | null>(null);
  protected readonly previewedSlot = signal<number | null>(null);
  protected readonly imagePreviewUrl = signal<string | null>(null);
  /** Also with a link that is not safe: shown without preview, it can still be deleted. */
  protected readonly hasCustomImage = signal(false);
  protected readonly isDeletionAsked = signal(false);
  protected readonly isImageTooLarge = signal(false);
  protected readonly isConflictShown = signal(false);
  /** The read after a version conflict failed: Modifica stays off. */
  private readonly readError = signal<ApiProblem | null>(null);
  protected readonly isLoaded = computed(
    () => this.settings() !== null && this.readError() === null,
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = this.errorText.message(this.error);
  private readonly saveError = signal<ApiProblem | null>(null);
  protected readonly saveErrorMessage = this.errorText.message(
    computed(() => this.saveError() ?? this.readError() ?? this.loadError()),
  );
  private savedSlot: number | null = null;
  private savedPreviewUrl: string | null = null;
  private savedHasCustomImage = false;
  /** `backgroundVersion` of the background shown, sent back with Salva. */
  private version: string | null = null;
  /** Address made in the browser for the last uploaded image: freed when it is replaced. */
  private localUrl: string | null = null;
  /** Address of the image still travelling: freed with its answer, or when the page closes first. */
  private uploadingUrl: string | null = null;
  private request?: Subscription;

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      const settings = this.settings();
      if (settings) {
        untracked(() => this.show(settings));
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.isEditing() &&
      (this.chosenSlot() !== this.savedSlot ||
        this.imagePreviewUrl() !== this.savedPreviewUrl ||
        this.hasCustomImage() !== this.savedHasCustomImage)
    );
  }

  protected edit(): void {
    if (this.isLoaded()) {
      this.isEditing.set(true);
    }
  }

  protected chooseSlot(slot: number): void {
    this.chosenSlot.set(slot);
    this.imagePreviewUrl.set(null);
    this.hasCustomImage.set(false);
    this.clearErrors();
  }

  protected showPreview(slot: number, event: PointerEvent): void {
    if (event.pointerType === 'mouse') {
      this.previewedSlot.set(slot);
    }
  }

  /** Sent at once, without Salva: the image shows from the browser while it travels. */
  protected chooseImage([image]: File[]): void {
    if (!image || this.isBusy()) {
      return;
    }
    this.clearErrors();
    if (image.size > MAX_IMAGE_UPLOAD_MEGABYTES * MEGABYTE) {
      this.isImageTooLarge.set(true);
      return;
    }
    if (!isUploadable(image, IMAGE_UPLOAD_TYPES, MAX_IMAGE_UPLOAD_MEGABYTES)) {
      this.saveError.set(INVALID_IMAGE);
      return;
    }
    const shownSlot = this.chosenSlot();
    const shownUrl = this.imagePreviewUrl();
    const shownCustomImage = this.hasCustomImage();
    const url = URL.createObjectURL(image);
    this.uploadingUrl = url;
    this.chosenSlot.set(null);
    this.imagePreviewUrl.set(url);
    this.hasCustomImage.set(true);
    this.isBusy.set(true);
    this.request = this.mapService.uploadBackground(image).subscribe({
      next: ({ backgroundVersion }) => {
        this.uploadingUrl = null;
        this.version = backgroundVersion;
        this.keepLocalUrl(url);
        this.saved(null, url, true);
      },
      error: (error: unknown) => {
        this.uploadingUrl = null;
        URL.revokeObjectURL(url);
        this.chosenSlot.set(shownSlot);
        this.imagePreviewUrl.set(shownUrl);
        this.hasCustomImage.set(shownCustomImage);
        this.isBusy.set(false);
        this.error.set(toApiProblem(error));
      },
    });
  }

  protected askDeletion(): void {
    if (this.isEditing() && !this.isBusy()) {
      this.isDeletionAsked.set(true);
    }
  }

  protected closeDeletion(isOpen: boolean): void {
    if (!isOpen) {
      this.isDeletionAsked.set(false);
    }
  }

  /** Deleted once confirmed, without Salva, only in Modifica. */
  protected confirmDeletion(): void {
    this.isDeletionAsked.set(false);
    if (!this.isEditing() || this.isBusy()) {
      return;
    }
    this.clearErrors();
    this.isBusy.set(true);
    this.request = this.mapService.removeBackground().subscribe({
      next: ({ backgroundVersion }) => {
        this.version = backgroundVersion;
        this.keepLocalUrl(null);
        this.saved(null, null, false);
      },
      error: (error: unknown) => {
        this.isBusy.set(false);
        this.error.set(toApiProblem(error));
      },
    });
  }

  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancel();
    }
  }

  protected cancel(): void {
    this.chosenSlot.set(this.savedSlot);
    this.imagePreviewUrl.set(this.savedPreviewUrl);
    this.hasCustomImage.set(this.savedHasCustomImage);
    this.clearErrors();
    this.isEditing.set(false);
  }

  /** Only a background of the frontend: the uploaded ones are saved when they are chosen. */
  protected save(): void {
    const slot = this.chosenSlot();
    const name = slot === null ? null : BACKGROUND_SLOTS[slot];
    if (this.isBusy() || !this.isLoaded()) {
      return;
    }
    this.clearErrors();
    if (!name || !this.hasUnsavedChanges()) {
      this.isEditing.set(false);
      return;
    }
    this.isBusy.set(true);
    this.isSaving.set(true);
    this.request = this.mapService.updateBackground(name, this.version).subscribe({
      next: (version) => {
        this.version = version;
        this.keepLocalUrl(null);
        this.saved(slot, null, false);
      },
      error: (error: unknown) => {
        const problem = toApiProblem(error);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictShown.set(true);
          this.reload();
          return;
        }
        this.isBusy.set(false);
        this.isSaving.set(false);
        this.saveError.set(problem);
      },
    });
  }

  private reload(): void {
    this.request = this.mapService.settings().subscribe({
      next: (settings) => this.show(settings),
      error: (error: unknown) => {
        this.cancel();
        this.isBusy.set(false);
        this.isSaving.set(false);
        this.readError.set(toApiProblem(error));
      },
    });
  }

  /** A name this page does not know chooses no slot. */
  private show({ background, backgroundVersion }: MapSettings): void {
    const slot = BACKGROUND_SLOTS.findIndex((name) => name !== null && name === background?.name);
    this.version = backgroundVersion;
    this.readError.set(null);
    this.keepLocalUrl(null);
    this.clearErrors();
    const isCustom = background?.isCustom ?? false;
    this.saved(
      slot < 0 ? null : slot,
      isCustom ? fileLinkOf(background?.url ?? null, this.origin) : null,
      isCustom,
    );
  }

  private saved(slot: number | null, previewUrl: string | null, hasCustomImage: boolean): void {
    this.savedSlot = slot;
    this.savedPreviewUrl = previewUrl;
    this.savedHasCustomImage = hasCustomImage;
    this.chosenSlot.set(slot);
    this.imagePreviewUrl.set(previewUrl);
    this.hasCustomImage.set(hasCustomImage);
    this.isBusy.set(false);
    this.isSaving.set(false);
    this.isEditing.set(false);
  }

  private keepLocalUrl(url: string | null): void {
    if (this.localUrl && this.localUrl !== url) {
      URL.revokeObjectURL(this.localUrl);
    }
    this.localUrl = url;
  }

  private clearErrors(): void {
    this.error.set(null);
    this.saveError.set(null);
    this.isImageTooLarge.set(false);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.request?.unsubscribe();
    this.keepLocalUrl(null);
    if (this.uploadingUrl) {
      URL.revokeObjectURL(this.uploadingUrl);
    }
  }
}
