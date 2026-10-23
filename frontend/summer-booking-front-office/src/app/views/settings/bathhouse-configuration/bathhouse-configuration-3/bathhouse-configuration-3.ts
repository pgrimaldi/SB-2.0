import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { GoogleMapsBehaviour } from '../../../../behaviours/maps/google-maps.behaviour';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { FilledTextField } from '../../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import {
  LocationMap,
  MapPosition,
} from '../../../../components/shared/ui/maps/location-map/location-map';
import { SectionTitle } from '../../../../components/shared/ui/titles/section-title/section-title';
import { FileDropZone } from '../../../../components/shared/ui/uploads/file-drop-zone/file-drop-zone';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { BathhouseMedia } from '../../../../entities/settings/bathhouse-configuration/bathhouse-media';
import { BathhousePhoto } from '../../../../entities/settings/bathhouse-configuration/bathhouse-photo';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';

const CENTER_OF_ITALY: MapPosition = { latitude: 41.87194, longitude: 12.56738 };
const ITALY_ZOOM = 5;
const POSITION_ZOOM = 15;
const GALLERY_ROW_SLOT_QUANTITY = 4;

@Component({
  selector: 'app-bathhouse-configuration-3',
  imports: [Button, FileDropZone, FilledTextField, LocationMap, SectionTitle, TranslatePipe],
  templateUrl: './bathhouse-configuration-3.html',
  styleUrl: './bathhouse-configuration-3.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseConfiguration3 implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly cache = inject(BathhouseConfigurationCacheBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly googleMaps = inject(GoogleMapsBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly origin = inject(DOCUMENT).location.origin;

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly positionIcon = ['/assets/images/BathhouseConfiguration_Position.svg'] as const;
  protected readonly imagesIcon = ['/assets/images/BathhouseConfiguration_Images.svg'] as const;
  protected readonly addImageIcon = ['/assets/images/BathhouseConfiguration_AddImage.svg'] as const;
  protected readonly virtualTourIcon = [
    '/assets/images/BathhouseConfiguration_VirtualTour.svg',
  ] as const;
  /** [content shown, content hidden, icon before the text]: only the link icon. */
  protected readonly virtualTourFieldIcons = [
    '',
    '',
    '/assets/images/BathhouseConfiguration_VirtualTourLink.svg',
  ] as const;
  protected readonly documentsIcon = [
    '/assets/images/BathhouseConfiguration_Documents.svg',
  ] as const;
  protected readonly uploadIcon = ['/assets/images/BathhouseConfiguration_Upload.svg'] as const;
  protected readonly mapId = environment.googleMapsMapId;
  protected readonly isMapReady = signal(false);
  protected readonly isMapFailed = signal(false);
  protected readonly position = signal<MapPosition | null>(null);
  protected readonly center = computed(() => this.position() ?? CENTER_OF_ITALY);
  protected readonly zoom = computed(() => (this.position() ? POSITION_ZOOM : ITALY_ZOOM));
  private readonly media = signal(new BathhouseMedia());
  protected readonly mainPhoto = computed(
    () => this.media().photos.find(({ isMain }) => isMain) ?? null,
  );
  protected readonly galleryPhotos = computed(() =>
    this.media().photos.filter(({ isMain }) => !isMain),
  );
  /** Rows of the gallery always full, with at least one free place. */
  protected readonly emptyGallerySlots = computed(() =>
    Array.from(
      {
        length:
          GALLERY_ROW_SLOT_QUANTITY - (this.galleryPhotos().length % GALLERY_ROW_SLOT_QUANTITY),
      },
      (_, index) => index,
    ),
  );
  protected readonly documents = computed(() => [
    {
      name: 'management.settings.bathhouse_configuration.media.documents.price_list',
      url: this.media().priceListPdfUrl,
    },
    {
      name: 'management.settings.bathhouse_configuration.media.documents.menu',
      url: this.media().menuPdfUrl,
    },
  ]);
  protected readonly isLoading = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly isEditing = signal(false);
  private loadedVirtualTourUrl: string | null = null;
  protected readonly virtualTourUrl = signal<string | null>(this.loadedVirtualTourUrl);

  constructor() {
    this.unsaved.watch(this);
    this.googleMaps.load().then(
      () => this.isMapReady.set(true),
      () => this.isMapFailed.set(true),
    );
    effect((onCleanup) => {
      const idProperty = this.auth.user()?.propertyId;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() => {
        this.isLoading.set(true);
        this.error.set(null);
        return this.cache.media(idProperty).subscribe({
          next: (answer) => {
            const media = mediaOf(answer, this.origin);
            this.media.set(media);
            this.loadedVirtualTourUrl = media.virtualTourUrl;
            this.virtualTourUrl.set(media.virtualTourUrl);
            this.isLoading.set(false);
          },
          error: (error: unknown) => {
            this.error.set(toApiProblem(error));
            this.isLoading.set(false);
          },
        });
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  hasUnsavedChanges(): boolean {
    return this.virtualTourUrl() !== this.loadedVirtualTourUrl;
  }

  protected edit(): void {
    this.isEditing.set(true);
  }

  protected cancel(): void {
    this.virtualTourUrl.set(this.loadedVirtualTourUrl);
    this.isEditing.set(false);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}

/** The links go into `src` and `href`: only https ones or ours are kept. */
function mediaOf(answer: BathhouseMedia, origin: string): BathhouseMedia {
  const media = onlyFieldsOf(BathhouseMedia, answer);
  return {
    ...media,
    photos: (media.photos ?? [])
      .map((photo) => onlyFieldsOf(BathhousePhoto, photo))
      .map((photo) => ({ ...photo, url: webLinkOf(photo.url, origin) }))
      .filter(({ url }) => url !== null),
    priceListPdfUrl: webLinkOf(media.priceListPdfUrl, origin),
    menuPdfUrl: webLinkOf(media.menuPdfUrl, origin),
  };
}

function webLinkOf(link: string | null, origin: string): string | null {
  if (!link) {
    return null;
  }
  try {
    const url = new URL(link, origin);
    return url.protocol === 'https:' || url.origin === origin ? url.href : null;
  } catch {
    return null;
  }
}
