import { DiscountScope } from '../../enums/discount-scope';
import { DiscountServiceCategory } from '../../enums/discount-service-category';
import { DiscountType } from '../../enums/discount-type';
import { VoucherValidityPeriod } from './voucher-validity-period';

export class VoucherSelectedItem {
  idVoucher: string | null = null;
  voucherName: string | null = null;
  voucherCode: string | null = null;
  isCodeRequired: boolean | null = null;
  validityPeriods: VoucherValidityPeriod[] = [];
  discountType: DiscountType | null = null;
  discountValue: number | null = null;
  discountScope: DiscountScope | null = null;
  serviceCategories: DiscountServiceCategory[] = [];
  idSectors: string[] = [];
  maxUseQuantity: number | null = null;
  isEnabled: boolean | null = null;
  version: string | null = null;
}
