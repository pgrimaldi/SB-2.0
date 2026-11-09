import { CdkConnectedOverlay, CdkOverlayOrigin, ConnectedPosition } from '@angular/cdk/overlay';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { MapBackgroundImage } from '../../../../../entities/enums/map-background-image';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  MAX_MAP_BACKGROUND_MEGABYTES,
  MapBackground,
  MapSettings,
} from '../../../../../entities/settings/map-configuration/map-settings';
import { MapService } from '../../../../../services/api/catalog/map.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Spinner } from '../../../../shared/ui/spinners/spinner/spinner';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { FileDropZone } from '../../../../shared/ui/uploads/file-drop-zone/file-drop-zone';

const BACKGROUND_SLOTS = [
  MapBackgroundImage.BackgroundBeach,
  MapBackgroundImage.BackgroundBoardwalk,
  null,
  null,
] as const;

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
    FileDropZone,
    IconButton,
    SectionTitle,
    Spinner,
    TranslatePipe,
  ],
  templateUrl: './map-background-panel.html',
  styleUrl: './map-background-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapBackgroundPanel implements UnsavedChanges, OnDestroy {
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly mapService = inject(MapService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly destroyRef = inject(DestroyRef);

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
  protected readonly deleteIcon = ['/assets/images/MapConfiguration_DeleteRed.svg'] as const;
  protected readonly maxImageMegabytes = MAX_MAP_BACKGROUND_MEGABYTES;
  protected readonly isEditing = signal(false);
  protected readonly isBusy = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly chosenSlot = signal<number | null>(null);
  protected readonly previewedSlot = signal<number | null>(null);
  protected readonly imagePreviewUrl = signal<string | null>(null);
  protected readonly isImageTooLarge = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private readonly saveError = signal<ApiProblem | null>(null);
  protected readonly saveErrorMessage = toSignal(
    toObservable(computed(() => this.saveError() ?? this.loadError())).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private savedSlot: number | null = null;
  private savedPreviewUrl: string | null = null;
  /** Address made in the browser for the last uploaded image: freed when it is replaced. */
  private localUrl: string | null = null;

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      const settings = this.settings();
      if (settings) {
        untracked(() => this.show(settings.background));
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.isEditing() &&
      (this.chosenSlot() !== this.savedSlot || this.imagePreviewUrl() !== this.savedPreviewUrl)
    );
  }

  protected edit(): void {
    this.isEditing.set(true);
  }

  protected chooseSlot(slot: number): void {
    this.chosenSlot.set(slot);
    this.imagePreviewUrl.set(null);
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
    if (image.size > MAX_MAP_BACKGROUND_MEGABYTES * 1024 * 1024) {
      this.isImageTooLarge.set(true);
      return;
    }
    const shownSlot = this.chosenSlot();
    const shownUrl = this.imagePreviewUrl();
    const url = URL.createObjectURL(image);
    this.chosenSlot.set(null);
    this.imagePreviewUrl.set(url);
    this.isBusy.set(true);
    this.mapService
      .uploadBackground(image)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.keepLocalUrl(url);
          this.saved(null, url);
        },
        error: (error: unknown) => {
          URL.revokeObjectURL(url);
          this.chosenSlot.set(shownSlot);
          this.imagePreviewUrl.set(shownUrl);
          this.isBusy.set(false);
          this.error.set(toApiProblem(error));
        },
      });
  }

  /** Deleted at once, without Salva nor confirmation. */
  protected removeImage(): void {
    if (this.isBusy()) {
      return;
    }
    this.clearErrors();
    this.isBusy.set(true);
    this.mapService
      .removeBackground()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.keepLocalUrl(null);
          this.saved(null, null);
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
    this.clearErrors();
    this.isEditing.set(false);
  }

  /** Only a background of the frontend: the uploaded ones are saved when they are chosen. */
  protected save(): void {
    const slot = this.chosenSlot();
    const name = slot === null ? null : BACKGROUND_SLOTS[slot];
    if (this.isBusy()) {
      return;
    }
    this.clearErrors();
    if (!name || !this.hasUnsavedChanges()) {
      this.isEditing.set(false);
      return;
    }
    this.isBusy.set(true);
    this.isSaving.set(true);
    this.mapService
      .updateBackground(name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.keepLocalUrl(null);
          this.saved(slot, null);
        },
        error: (error: unknown) => {
          this.isBusy.set(false);
          this.isSaving.set(false);
          this.saveError.set(toApiProblem(error));
        },
      });
  }

  /** A name this page does not know chooses no slot. */
  private show(background: MapBackground | null): void {
    const slot = BACKGROUND_SLOTS.findIndex((name) => name !== null && name === background?.name);
    this.keepLocalUrl(null);
    this.clearErrors();
    this.saved(slot < 0 ? null : slot, background?.isCustom ? background.url : null);
  }

  private saved(slot: number | null, previewUrl: string | null): void {
    this.savedSlot = slot;
    this.savedPreviewUrl = previewUrl;
    this.chosenSlot.set(slot);
    this.imagePreviewUrl.set(previewUrl);
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
    this.keepLocalUrl(null);
  }
}
