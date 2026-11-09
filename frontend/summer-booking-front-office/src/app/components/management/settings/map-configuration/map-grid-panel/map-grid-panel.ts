import { NgTemplateOutlet } from '@angular/common';
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
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  TreeValidationResult,
  form,
  max,
  min,
  readonly,
  required,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { MapMixedSpotIcon } from '../../../../../entities/enums/map-mixed-spot-icon';
import { MapUmbrellaIcon } from '../../../../../entities/enums/map-umbrella-icon';
import {
  DEFAULT_MAP_GRID,
  MAX_MAP_ROW_ITEM_QUANTITY,
  MAX_MAP_ROW_QUANTITY,
  MAX_MAP_TILT_DEGREES,
  MapArticleIcons,
  MapElementIcons,
  MapGrid,
} from '../../../../../entities/settings/map-configuration/map-grid';
import { MapSettings } from '../../../../../entities/settings/map-configuration/map-settings';
import { MapService } from '../../../../../services/api/catalog/map.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import {
  IconPickerPopup,
  IconPickerPopupIcon,
} from '../../../../shared/ui/dialogs/icon-picker-popup/icon-picker-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

const CUSTOM_ARTICLE_NUMBERS = [1, 2, 3, 4, 5, 6, 7, 8] as const;

/** The icon being chosen: `article` null is the umbrella. */
interface IconPicking {
  article: number | null;
  isOccupied: boolean;
}

const RED_CLOSE_ICON = '/assets/images/MapConfiguration_CloseRed.svg';

const OCCUPIED_UMBRELLA_ICONS = [
  MapMixedSpotIcon.MixedSpot3,
  MapMixedSpotIcon.MixedSpot4,
  MapMixedSpotIcon.MixedSpot9,
  MapMixedSpotIcon.MixedSpot13,
  MapMixedSpotIcon.MixedSpot16,
  MapMixedSpotIcon.MixedSpot18,
  MapMixedSpotIcon.MixedSpot19,
  MapMixedSpotIcon.MixedSpot32,
  MapMixedSpotIcon.MixedSpot36,
];

/** The icons a button can show: `article` null is the umbrella. */
function choicesOf(article: number | null, isOccupied: boolean): readonly string[] {
  if (article !== null) {
    return Object.values(MapMixedSpotIcon);
  }
  return isOccupied ? OCCUPIED_UMBRELLA_ICONS : Object.values(MapUmbrellaIcon);
}

function iconsOf<K extends string>(icons: readonly K[]): IconPickerPopupIcon<K>[] {
  return icons.map((value) => ({
    value,
    url: `/assets/images/MapConfiguration_${value}.svg`,
  }));
}

@Component({
  selector: 'app-map-grid-panel',
  imports: [
    Button,
    FilledNumberField,
    FormField,
    IconButton,
    IconPickerPopup,
    NgTemplateOutlet,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './map-grid-panel.html',
  styleUrl: './map-grid-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapGridPanel implements UnsavedChanges, OnDestroy {
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly mapService = inject(MapService);
  private readonly errorText = inject(ErrorTextBehaviour);

  readonly settings = input<MapSettings | null>(null);
  readonly loadError = input<ApiProblem | null>(null);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly customArticles = CUSTOM_ARTICLE_NUMBERS;
  /** `isOccupied` of the two columns, Libero and Occupato. */
  protected readonly iconColumns = [false, true] as const;
  protected readonly umbrellaIcons = iconsOf(Object.values(MapUmbrellaIcon));
  private readonly mixedSpotIcons = iconsOf(Object.values(MapMixedSpotIcon));
  private readonly occupiedUmbrellaIcons = iconsOf(OCCUPIED_UMBRELLA_ICONS);
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly emptyIcon = [RED_CLOSE_ICON] as const;
  protected readonly titleIcon = ['/assets/images/MapConfiguration_Grid.svg'] as const;
  /** Only the icon before the number. */
  protected readonly tiltIcons = ['', '', '', '/assets/images/MapConfiguration_Tilt.svg'] as const;
  protected readonly isEditing = signal(false);
  protected readonly isSaving = signal(false);
  private saved: MapGrid = { ...DEFAULT_MAP_GRID };
  private readonly grid = signal<MapGrid>({ ...DEFAULT_MAP_GRID });
  protected readonly gridForm = form(this.grid, (path) => {
    required(path.rowQuantity);
    min(path.rowQuantity, 1);
    max(path.rowQuantity, MAX_MAP_ROW_QUANTITY);
    required(path.maxRowItemQuantity);
    min(path.maxRowItemQuantity, 1);
    max(path.maxRowItemQuantity, MAX_MAP_ROW_ITEM_QUANTITY);
    required(path.tiltDegrees);
    max(path.tiltDegrees, MAX_MAP_TILT_DEGREES);
    readonly(path, () => !this.isEditing() || this.isSaving());
  });
  protected readonly fieldErrors = {
    rowQuantity: this.validationText.message(this.gridForm.rowQuantity),
    maxRowItemQuantity: this.validationText.message(this.gridForm.maxRowItemQuantity),
    tiltDegrees: this.validationText.message(this.gridForm.tiltDegrees),
  };
  private readonly hasUnknownIcon = computed(() =>
    [null, ...CUSTOM_ARTICLE_NUMBERS].some((article) =>
      this.iconColumns.some((isOccupied) => this.isUnknownIcon(article, isOccupied)),
    ),
  );
  protected readonly canSave = computed(
    () => this.gridForm().valid() && !this.hasUnknownIcon() && !this.isSaving(),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(computed(() => this.error() ?? this.loadError())).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly picking = signal<IconPicking | null>(null);
  protected readonly isPickingUmbrella = computed(() => {
    const picking = this.picking();
    return picking?.article === null && !picking.isOccupied;
  });
  protected readonly isPickingMixedSpot = computed(
    () => this.picking() !== null && !this.isPickingUmbrella(),
  );
  protected readonly mixedSpotChoices = computed(() =>
    this.picking()?.article ? this.mixedSpotIcons : this.occupiedUmbrellaIcons,
  );
  protected readonly pickedUmbrellaIcon = computed(() =>
    this.isPickingUmbrella() ? this.grid().umbrellaIcons.freeIcon : null,
  );
  protected readonly pickedMixedSpotIcon = computed(() => {
    const picking = this.picking();
    if (!picking || this.isPickingUmbrella()) {
      return null;
    }
    return picking.article
      ? this.iconOf(this.articleIconsOf(picking.article), picking.isOccupied)
      : this.grid().umbrellaIcons.occupiedIcon;
  });

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      const settings = this.settings();
      if (settings) {
        untracked(() => {
          this.saved = this.gridOf(settings);
          this.cancel();
        });
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.gridForm().dirty();
  }

  protected edit(): void {
    this.isEditing.set(true);
  }

  protected chosenIconUrl(article: number | null, isOccupied: boolean): string | null {
    const icon = this.iconAt(article, isOccupied);
    if (icon === null) {
      return null;
    }
    return choicesOf(article, isOccupied).includes(icon)
      ? `/assets/images/MapConfiguration_${icon}.svg`
      : RED_CLOSE_ICON;
  }

  /** A name the backend sent that is not among the icons of the button. */
  protected isUnknownIcon(article: number | null, isOccupied: boolean): boolean {
    const icon = this.iconAt(article, isOccupied);
    return icon !== null && !choicesOf(article, isOccupied).includes(icon);
  }

  protected chooseUmbrellaIcon(icon: MapUmbrellaIcon): void {
    const field = this.gridForm.umbrellaIcons.freeIcon;
    field().value.set(icon);
    field().markAsDirty();
  }

  protected emptyIconButton(article: number | null, isOccupied: boolean): void {
    if (!this.isEditing() || this.isSaving()) {
      return;
    }
    const icons =
      article === null
        ? this.gridForm.umbrellaIcons
        : this.gridForm.customArticleIcons[article - 1];
    const field = isOccupied ? icons.occupiedIcon : icons.freeIcon;
    field().value.set(null);
    field().markAsDirty();
  }

  protected chooseMixedSpotIcon(icon: MapMixedSpotIcon): void {
    const picking = this.picking();
    if (!picking) {
      return;
    }
    const icons = picking.article ? this.gridForm.customArticleIcons[picking.article - 1] : null;
    const field = !icons
      ? this.gridForm.umbrellaIcons.occupiedIcon
      : picking.isOccupied
        ? icons.occupiedIcon
        : icons.freeIcon;
    field().value.set(icon);
    field().markAsDirty();
  }

  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancel();
    }
  }

  protected cancel(): void {
    this.grid.set(this.saved);
    this.gridForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  /** Nothing changed: no call. Through submit(): an error of the backend on a field goes under it. */
  protected save(): void {
    if (!this.canSave()) {
      return;
    }
    this.error.set(null);
    if (!this.gridForm().dirty()) {
      this.isEditing.set(false);
      return;
    }
    void submit(this.gridForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      try {
        const sent = this.grid();
        await firstValueFrom(this.mapService.updateConfiguration(sent));
        this.saved = sent;
        this.gridForm().reset();
        this.isEditing.set(false);
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.gridForm, (general) =>
          this.error.set(general),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  /** Names as they arrive: a name this page does not know stays, shown as unknown. */
  private gridOf({ rows, columns, tiltDegrees, icons }: MapSettings): MapGrid {
    return {
      rowQuantity: rows ?? DEFAULT_MAP_GRID.rowQuantity,
      maxRowItemQuantity: columns ?? DEFAULT_MAP_GRID.maxRowItemQuantity,
      tiltDegrees: tiltDegrees ?? DEFAULT_MAP_GRID.tiltDegrees,
      umbrellaIcons: {
        freeIcon: icons.umbrella.free.name as MapUmbrellaIcon | null,
        occupiedIcon: icons.umbrella.occupied.name as MapMixedSpotIcon | null,
      },
      customArticleIcons: DEFAULT_MAP_GRID.customArticleIcons.map(({ variant }) => {
        const pair = icons.customArticles.find((article) => article.variant === variant)?.icons;
        return {
          variant,
          freeIcon: (pair?.free.name ?? null) as MapMixedSpotIcon | null,
          occupiedIcon: (pair?.occupied.name ?? null) as MapMixedSpotIcon | null,
        };
      }),
    };
  }

  private iconAt(article: number | null, isOccupied: boolean): string | null {
    const icons: MapElementIcons<string> =
      article === null ? this.grid().umbrellaIcons : this.articleIconsOf(article);
    return this.iconOf(icons, isOccupied);
  }

  private articleIconsOf(article: number): MapArticleIcons {
    return this.grid().customArticleIcons[article - 1];
  }

  private iconOf<F, O>(icons: MapElementIcons<F, O>, isOccupied: boolean): F | O | null {
    return isOccupied ? icons.occupiedIcon : icons.freeIcon;
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}
