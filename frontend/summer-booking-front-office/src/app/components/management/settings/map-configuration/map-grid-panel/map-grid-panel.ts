import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormField, form, max, readonly, required } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import {
  DEFAULT_MAP_GRID,
  MAX_MAP_ROW_ITEM_QUANTITY,
  MAX_MAP_ROW_QUANTITY,
  MAX_MAP_TILT_DEGREES,
  MapGrid,
} from '../../../../../entities/settings/map-configuration/map-grid';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

const CUSTOM_ARTICLE_NUMBERS = [1, 2, 3, 4, 5, 6, 7, 8] as const;

@Component({
  selector: 'app-map-grid-panel',
  imports: [Button, FilledNumberField, FormField, NgTemplateOutlet, SectionTitle, TranslatePipe],
  templateUrl: './map-grid-panel.html',
  styleUrl: './map-grid-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapGridPanel implements UnsavedChanges, OnDestroy {
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly customArticles = CUSTOM_ARTICLE_NUMBERS;
  protected readonly titleIcon = ['/assets/images/MapConfiguration_Grid.svg'] as const;
  /** Only the icon before the number. */
  protected readonly tiltIcons = ['', '', '', '/assets/images/MapConfiguration_Tilt.svg'] as const;
  protected readonly isEditing = signal(false);
  private saved: MapGrid = { ...DEFAULT_MAP_GRID };
  private readonly grid = signal<MapGrid>({ ...DEFAULT_MAP_GRID });
  protected readonly gridForm = form(this.grid, (path) => {
    required(path.rowQuantity);
    max(path.rowQuantity, MAX_MAP_ROW_QUANTITY);
    required(path.maxRowItemQuantity);
    max(path.maxRowItemQuantity, MAX_MAP_ROW_ITEM_QUANTITY);
    required(path.tiltDegrees);
    max(path.tiltDegrees, MAX_MAP_TILT_DEGREES);
    readonly(path, () => !this.isEditing());
  });
  protected readonly fieldErrors = {
    rowQuantity: this.validationText.message(this.gridForm.rowQuantity),
    maxRowItemQuantity: this.validationText.message(this.gridForm.maxRowItemQuantity),
    tiltDegrees: this.validationText.message(this.gridForm.tiltDegrees),
  };
  protected readonly canSave = computed(() => this.gridForm().valid());

  constructor() {
    this.unsaved.watch(this);
  }

  hasUnsavedChanges(): boolean {
    return this.gridForm().dirty();
  }

  protected edit(): void {
    this.isEditing.set(true);
  }

  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancel();
    }
  }

  protected cancel(): void {
    this.grid.set(this.saved);
    this.gridForm().reset();
    this.isEditing.set(false);
  }

  protected save(): void {
    if (!this.canSave()) {
      return;
    }
    this.saved = this.grid();
    this.gridForm().reset();
    this.isEditing.set(false);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}
