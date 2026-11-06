import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { FileDropZone } from '../../../../shared/ui/uploads/file-drop-zone/file-drop-zone';

const BACKGROUND_SLOTS = [1, 2, 3, 4] as const;

@Component({
  selector: 'app-map-background-panel',
  imports: [Button, FileDropZone, SectionTitle, TranslatePipe],
  templateUrl: './map-background-panel.html',
  styleUrl: './map-background-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapBackgroundPanel implements UnsavedChanges, OnDestroy {
  private readonly unsaved = inject(UnsavedChangesBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly slots = BACKGROUND_SLOTS;
  protected readonly chosenIcon = '/assets/images/MapConfiguration_Chosen.svg';
  protected readonly uploadIcon = ['/assets/images/GeneralIcon_AddImage.svg'] as const;
  protected readonly isEditing = signal(false);
  protected readonly chosenSlot = signal<number | null>(null);
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
