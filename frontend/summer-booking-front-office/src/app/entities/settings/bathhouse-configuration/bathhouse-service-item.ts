import { BathhouseServiceGroup } from '../../enums/bathhouse-service-group';

export class BathhouseServiceItem {
  idService: number | null = null;
  name: string | null = null;
  categoryCode: BathhouseServiceGroup | null = null;
  iconUrl: string | null = null;
  isSelected = false;
}
