import { DiscountScope } from '../../enums/discount-scope';
import { DiscountServiceCategory } from '../../enums/discount-service-category';
import { DiscountType } from '../../enums/discount-type';
import { VoucherValidityPeriod } from './voucher-validity-period';

export class VoucherUpdateItemRequest {
  idVoucher: string | null = null;
  voucherName: string | null = null;
  voucherCode: string | null = null;
  isCodeRequired = true;
  validityPeriods: VoucherValidityPeriod[] = [];
  discountType: DiscountType = DiscountType.Percentage;
  discountValue: number | null = null;
  discountScope: DiscountScope = DiscountScope.All;
  serviceCategories: DiscountServiceCategory[] = [];
  idSectors: string[] = [];
  maxUseQuantity: number | null = 1;
  isEnabled = true;
}
