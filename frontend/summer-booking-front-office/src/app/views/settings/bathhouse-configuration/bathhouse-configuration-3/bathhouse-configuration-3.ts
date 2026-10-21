import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  inject,
  signal,
} from '@angular/core';
import { GoogleMap, MapAdvancedMarker } from '@angular/google-maps';
import { TranslatePipe } from '@ngx-translate/core';
import { environment } from '../../../../../environments/environment';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { GoogleMapsBehaviour } from '../../../../behaviours/maps/google-maps.behaviour';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { FilledTextField } from '../../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import { SectionTitle } from '../../../../components/shared/ui/titles/section-title/section-title';
import { FileDropZone } from '../../../../components/shared/ui/uploads/file-drop-zone/file-drop-zone';

const CENTER_OF_ITALY: google.maps.LatLngLiteral = { lat: 41.87194, lng: 12.56738 };
const ITALY_ZOOM = 5;
const POSITION_ZOOM = 15;
const GALLERY_SLOT_QUANTITY = 4;

@Component({
  selector: 'app-bathhouse-configuration-3',
  imports: [
    Button,
    FileDropZone,
    FilledTextField,
    GoogleMap,
    MapAdvancedMarker,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './bathhouse-configuration-3.html',
  styleUrl: './bathhouse-configuration-3.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseConfiguration3 implements UnsavedChanges, OnDestroy {
  private readonly googleMaps = inject(GoogleMapsBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);

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
  protected readonly mapOptions: google.maps.MapOptions = {
    disableDefaultUI: true,
    fullscreenControl: true,
    clickableIcons: false,
  };
  protected readonly gallerySlots = Array.from(
    { length: GALLERY_SLOT_QUANTITY },
    (_, index) => index,
  );
  protected readonly documents = [
    'management.settings.bathhouse_configuration.media.documents.price_list',
    'management.settings.bathhouse_configuration.media.documents.menu',
  ] as const;
  protected readonly isMapReady = signal(false);
  protected readonly isMapFailed = signal(false);
  protected readonly position = signal<google.maps.LatLngLiteral | null>(null);
  protected readonly center = computed(() => this.position() ?? CENTER_OF_ITALY);
  protected readonly zoom = computed(() => (this.position() ? POSITION_ZOOM : ITALY_ZOOM));
  protected readonly isEditing = signal(false);
  private loadedVirtualTourUrl: string | null = null;
  protected readonly virtualTourUrl = signal<string | null>(this.loadedVirtualTourUrl);

  constructor() {
    this.unsaved.watch(this);
    this.googleMaps.load().then(
      () => this.isMapReady.set(true),
      () => this.isMapFailed.set(true),
    );
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
