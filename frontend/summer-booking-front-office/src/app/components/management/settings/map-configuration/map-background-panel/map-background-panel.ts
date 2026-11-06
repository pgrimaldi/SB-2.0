import { CdkConnectedOverlay, CdkOverlayOrigin, ConnectedPosition } from '@angular/cdk/overlay';
import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { FileDropZone } from '../../../../shared/ui/uploads/file-drop-zone/file-drop-zone';

const BACKGROUND_SLOTS = [
  '/assets/images/MapConfiguration_BackgroundBeach.png',
  '/assets/images/MapConfiguration_BackgroundBoardwalk.png',
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
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './map-background-panel.html',
  styleUrl: './map-background-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapBackgroundPanel implements UnsavedChanges, OnDestroy {
  private readonly unsaved = inject(UnsavedChangesBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly slots = BACKGROUND_SLOTS;
  protected readonly previewPositions = PREVIEW_POSITIONS;
  protected readonly titleIcon = ['/assets/images/MapConfiguration_Palette.svg'] as const;
  protected readonly chosenIcon = '/assets/images/MapConfiguration_Chosen.svg';
  protected readonly uploadIcon = ['/assets/images/GeneralIcon_AddImage.svg'] as const;
  protected readonly isEditing = signal(false);
  protected readonly chosenSlot = signal<number | null>(null);
  protected readonly previewedSlot = signal<number | null>(null);
  protected readonly imagePreviewUrl = signal<string | null>(null);
  private readonly image = signal<File | null>(null);
  private savedSlot: number | null = null;
  private savedImage: File | null = null;

  constructor() {
    this.unsaved.watch(this);
  }

  hasUnsavedChanges(): boolean {
    return (
      this.isEditing() && (this.chosenSlot() !== this.savedSlot || this.image() !== this.savedImage)
    );
  }

  protected edit(): void {
    this.isEditing.set(true);
  }

  protected chooseSlot(slot: number): void {
    this.chosenSlot.set(slot);
    this.setImage(null);
  }

  protected showPreview(slot: number, event: PointerEvent): void {
    if (event.pointerType === 'mouse') {
      this.previewedSlot.set(slot);
    }
  }

  protected chooseImage([image]: File[]): void {
    if (image) {
      this.chosenSlot.set(null);
      this.setImage(image);
    }
  }

  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancel();
    }
  }

  protected cancel(): void {
    this.chosenSlot.set(this.savedSlot);
    this.setImage(this.savedImage);
    this.isEditing.set(false);
  }

  protected save(): void {
    this.savedSlot = this.chosenSlot();
    this.savedImage = this.image();
    this.isEditing.set(false);
  }

  private setImage(image: File | null): void {
    const previewUrl = this.imagePreviewUrl();
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }
    this.image.set(image);
    this.imagePreviewUrl.set(image ? URL.createObjectURL(image) : null);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.setImage(null);
  }
}
