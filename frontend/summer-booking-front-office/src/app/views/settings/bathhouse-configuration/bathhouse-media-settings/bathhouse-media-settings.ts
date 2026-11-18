import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  Signal,
  WritableSignal,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, TreeValidationResult, form, readonly, submit } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, firstValueFrom, of, switchMap } from 'rxjs';
import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import { environment } from '../../../../../environments/environment';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { GoogleMapsBehaviour } from '../../../../behaviours/maps/google-maps.behaviour';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../behaviours/validation/validation-text.behaviour';
import { webLink } from '../../../../components/shared/forms/validators';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { IconButton } from '../../../../components/shared/ui/buttons/icon-button/icon-button';
import { ConfirmPopup } from '../../../../components/shared/ui/dialogs/confirm-popup/confirm-popup';
import { FilledTextField } from '../../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import {
  LocationMap,
  MapPosition,
} from '../../../../components/shared/ui/maps/location-map/location-map';
import { PageSpinner } from '../../../../components/shared/ui/spinners/page-spinner/page-spinner';
import { SectionTitle } from '../../../../components/shared/ui/titles/section-title/section-title';
import { FileDropZone } from '../../../../components/shared/ui/uploads/file-drop-zone/file-drop-zone';
import { BathhouseDocument } from '../../../../entities/enums/bathhouse-document';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import {
  BathhouseMedia,
  MAX_BATHHOUSE_IMAGE_MEGABYTES,
  MAX_BATHHOUSE_PDF_MEGABYTES,
  MAX_BATHHOUSE_PHOTO_QUANTITY,
} from '../../../../entities/settings/bathhouse-configuration/bathhouse-media';
import { BathhouseMediaRequest } from '../../../../entities/settings/bathhouse-configuration/bathhouse-media-request';
import { BathhousePhoto } from '../../../../entities/settings/bathhouse-configuration/bathhouse-photo';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';

const CENTER_OF_ITALY: MapPosition = { latitude: 41.87194, longitude: 12.56738 };
const ITALY_ZOOM = 5;
const POSITION_ZOOM = 15;
/** The backend keeps 6 decimals of latitude and longitude (about 10 cm). */
const COORDINATE_FACTOR = 1_000_000;
const GALLERY_ROW_SLOT_QUANTITY = 4;
const MEGABYTE = 1024 * 1024;
/** The images the backend takes, as in `accept`. */
const IMAGE_TYPES = ['image/png', 'image/jpeg', 'image/webp'];
const PDF_TYPE = 'application/pdf';
/** The same answers the backend gives, before sending a file it would refuse. */
const INVALID_IMAGE: ApiProblem = {
  status: 422,
  code: ApiErrorCode.CatalogInvalidImage,
  args: { maxBytes: MAX_BATHHOUSE_IMAGE_MEGABYTES * MEGABYTE },
};
const INVALID_PDF: ApiProblem = {
  status: 422,
  code: ApiErrorCode.CatalogInvalidPdf,
  args: { maxBytes: MAX_BATHHOUSE_PDF_MEGABYTES * MEGABYTE },
};
const GALLERY_FULL: ApiProblem = {
  status: 422,
  code: ApiErrorCode.CatalogGalleryFull,
  args: { max: MAX_BATHHOUSE_PHOTO_QUANTITY },
};

/** A call of the tab: each answers with all the media of the property. */
type MediaCall = () => Observable<BathhouseMedia>;

@Component({
  selector: 'app-bathhouse-media-settings',
  imports: [
    Button,
    ConfirmPopup,
    FileDropZone,
    FilledTextField,
    FormField,
    IconButton,
    LocationMap,
    PageSpinner,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './bathhouse-media-settings.html',
  styleUrl: './bathhouse-media-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseMediaSettings implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly cache = inject(BathhouseConfigurationCacheBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly googleMaps = inject(GoogleMapsBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly origin = inject(DOCUMENT).location.origin;

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly positionIcon = ['/assets/images/BathhouseConfiguration_Position.svg'] as const;
  protected readonly imagesIcon = ['/assets/images/GeneralIcon_Images.svg'] as const;
  protected readonly addImageIcon = ['/assets/images/GeneralIcon_AddImage.svg'] as const;
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
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [X]. */
  protected readonly popupIcons = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly imageTypes = IMAGE_TYPES.join(',');
  protected readonly pdfType = PDF_TYPE;
  protected readonly mapId = environment.googleMapsMapId;
  protected readonly isMapReady = signal(false);
  protected readonly isMapFailed = signal(false);
  private loaded = new BathhouseMediaRequest();
  private readonly request = signal(new BathhouseMediaRequest());
  protected readonly mediaForm = form(this.request, (path) => {
    webLink(path.virtualTourUrl);
    readonly(path, () => !this.isEditing() || this.isSaving());
  });
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.request()) as (keyof BathhouseMediaRequest)[]).map((name) => [
      name,
      this.validationText.message(this.mediaForm[name]),
    ]),
  ) as Readonly<Record<keyof BathhouseMediaRequest, Signal<string | null>>>;
  protected readonly position = computed<MapPosition | null>(() => {
    const { latitude, longitude } = this.request();
    return latitude !== null && longitude !== null ? { latitude, longitude } : null;
  });
  protected readonly center = computed(() => this.position() ?? CENTER_OF_ITALY);
  protected readonly zoom = computed(() => (this.position() ? POSITION_ZOOM : ITALY_ZOOM));
  private readonly media = signal(new BathhouseMedia());
  protected readonly mainPhotoUrl = computed(() => this.media().mainPhotoUrl);
  protected readonly galleryPhotos = computed(() => this.media().photos);
  /** Rows of the gallery always full, with at least one free place until the gallery is full. */
  protected readonly emptyGallerySlots = computed(() => {
    const photoQuantity = this.galleryPhotos().length;
    const slotQuantity = Math.min(
      GALLERY_ROW_SLOT_QUANTITY - (photoQuantity % GALLERY_ROW_SLOT_QUANTITY),
      MAX_BATHHOUSE_PHOTO_QUANTITY - photoQuantity,
    );
    return Array.from({ length: Math.max(slotQuantity, 0) }, (_, index) => index);
  });
  protected readonly documents = computed(() => [
    {
      kind: BathhouseDocument.PriceList,
      name: 'management.settings.bathhouse_configuration.media.documents.price_list',
      url: this.media().priceListPdfUrl,
      fileName: this.media().priceListPdfFileName,
    },
    {
      kind: BathhouseDocument.Menu,
      name: 'management.settings.bathhouse_configuration.media.documents.menu',
      url: this.media().menuPdfUrl,
      fileName: this.media().menuPdfFileName,
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
  protected readonly isSaving = signal(false);
  /** A photo or a PDF is being uploaded or deleted: they are sent at once, one at a time. */
  protected readonly isActionRunning = signal(false);
  /** What the X of a photo or of a PDF deletes, until the popup is answered. */
  protected readonly deletion = signal<MediaCall | null>(null);
  protected readonly isDocumentDeletion = signal(false);
  private readonly imagesError = signal<ApiProblem | null>(null);
  protected readonly imagesErrorMessage = this.messageOf(this.imagesError);
  private readonly documentsError = signal<ApiProblem | null>(null);
  protected readonly documentsErrorMessage = this.messageOf(this.documentsError);

  constructor() {
    this.unsaved.watch(this);
    this.googleMaps.load().then(
      () => this.isMapReady.set(true),
      () => this.isMapFailed.set(true),
    );
    effect((onCleanup) => {
      if (!this.auth.user()) {
        return;
      }
      const subscription = untracked(() => {
        this.isLoading.set(true);
        this.error.set(null);
        return this.cache.media().subscribe({
          next: (answer) => {
            const media = mediaOf(answer, this.origin);
            this.media.set(media);
            this.loaded = onlyFieldsOf(BathhouseMediaRequest, media);
            this.request.set(this.loaded);
            this.mediaForm().reset();
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
    const current = this.request();
    return (Object.keys(this.loaded) as (keyof BathhouseMediaRequest)[]).some(
      (name) => current[name] !== this.loaded[name],
    );
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
    this.request.set(this.loaded);
    this.mediaForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  protected save(): void {
    if (this.isSaving()) {
      return;
    }
    void submit(this.mediaForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const sent = {
          ...this.request(),
          virtualTourUrl: this.request().virtualTourUrl?.trim() || null,
        };
        await firstValueFrom(this.catalog.saveBathhouseConfigurationMedia(sent));
        this.loaded = sent;
        this.request.set(sent);
        this.media.update((media) => ({ ...media, ...sent }));
        this.cache.keepMedia(this.media());
        this.mediaForm().reset();
        this.isEditing.set(false);
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.mediaForm, (general) =>
          this.error.set(general),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  /** In Modifica a click on the map moves the position of the bathhouse there. */
  protected placePosition({ latitude, longitude }: MapPosition): void {
    if (!this.isEditing() || this.isSaving()) {
      return;
    }
    this.mediaForm.latitude().value.set(roundCoordinate(latitude));
    this.mediaForm.longitude().value.set(roundCoordinate(longitude));
    this.mediaForm.latitude().markAsDirty();
  }

  protected uploadMainPhoto([image]: File[]): void {
    if (!image) {
      return;
    }
    if (!isImage(image)) {
      this.imagesError.set(INVALID_IMAGE);
      return;
    }
    void this.run([() => this.catalog.uploadBathhouseMainPhoto(image)], this.imagesError);
  }

  /** The valid images, as long as the gallery has room; then the reason of those left out. */
  protected addPhotos(images: File[]): void {
    const valid = images.filter(isImage);
    const room = MAX_BATHHOUSE_PHOTO_QUANTITY - this.galleryPhotos().length;
    const leftOut =
      valid.length < images.length ? INVALID_IMAGE : valid.length > room ? GALLERY_FULL : null;
    void this.run(
      valid.slice(0, Math.max(room, 0)).map((image) => () => this.catalog.addBathhousePhoto(image)),
      this.imagesError,
      leftOut,
    );
  }

  protected uploadDocument(document: BathhouseDocument, input: HTMLInputElement): void {
    const [pdf] = Array.from(input.files ?? []);
    input.value = '';
    if (!pdf) {
      return;
    }
    if (pdf.type !== PDF_TYPE || pdf.size > MAX_BATHHOUSE_PDF_MEGABYTES * MEGABYTE) {
      this.documentsError.set(INVALID_PDF);
      return;
    }
    void this.run([() => this.catalog.uploadBathhouseDocument(document, pdf)], this.documentsError);
  }

  protected askMainPhotoDeletion(): void {
    this.askDeletion(() => this.catalog.deleteBathhouseMainPhoto(), false);
  }

  protected askPhotoDeletion(idPhoto: string | null): void {
    if (idPhoto) {
      this.askDeletion(() => this.catalog.deleteBathhousePhoto(idPhoto), false);
    }
  }

  protected askDocumentDeletion(document: BathhouseDocument): void {
    this.askDeletion(() => this.catalog.deleteBathhouseDocument(document), true);
  }

  protected confirmDeletion(): void {
    const deletion = this.deletion();
    this.deletion.set(null);
    if (deletion) {
      void this.run([deletion], this.isDocumentDeletion() ? this.documentsError : this.imagesError);
    }
  }

  protected closeDeletion(isOpen: boolean): void {
    if (!isOpen) {
      this.deletion.set(null);
    }
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }

  private askDeletion(call: MediaCall, isDocument: boolean): void {
    this.isDocumentDeletion.set(isDocument);
    this.deletion.set(call);
  }

  /**
   * The calls one after the other, each answer shown at once; the first error stops them. Without
   * errors, `afterwards` (e.g. files left out) is the message of the section.
   */
  private async run(
    calls: readonly MediaCall[],
    error: WritableSignal<ApiProblem | null>,
    afterwards: ApiProblem | null = null,
  ): Promise<void> {
    if (this.isActionRunning()) {
      return;
    }
    this.imagesError.set(null);
    this.documentsError.set(null);
    this.isActionRunning.set(true);
    try {
      for (const call of calls) {
        this.keep(await firstValueFrom(call()));
      }
      error.set(afterwards);
    } catch (caught: unknown) {
      error.set(toApiProblem(caught));
    } finally {
      this.isActionRunning.set(false);
    }
  }

  /** The link of the virtual tour and the position stay the saved ones: they are saved apart. */
  private keep(answer: BathhouseMedia): void {
    const { virtualTourUrl, latitude, longitude } = this.media();
    const media = { ...mediaOf(answer, this.origin), virtualTourUrl, latitude, longitude };
    this.media.set(media);
    this.cache.keepMedia(media);
  }

  private messageOf(problem: Signal<ApiProblem | null>): Signal<string | null> {
    return toSignal(
      toObservable(problem).pipe(
        switchMap((current) => (current ? this.errorText.text(current) : of(null))),
      ),
      { initialValue: null },
    );
  }
}

/** The links go into `src` and `href`: only https ones or ours are kept. */
function mediaOf(answer: BathhouseMedia, origin: string): BathhouseMedia {
  const media = onlyFieldsOf(BathhouseMedia, answer);
  return {
    ...media,
    mainPhotoUrl: webLinkOf(media.mainPhotoUrl, origin),
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

function roundCoordinate(value: number): number {
  return Math.round(value * COORDINATE_FACTOR) / COORDINATE_FACTOR;
}

function isImage(file: File): boolean {
  return IMAGE_TYPES.includes(file.type) && file.size <= MAX_BATHHOUSE_IMAGE_MEGABYTES * MEGABYTE;
}
