import { BarServiceCategory } from '../../enums/bar-service-category';

export class BarServiceItem {
  idBarService: string | null = null;
  serviceName: string | null = null;
  category: BarServiceCategory | null = null;
  price: number | null = null;
}
