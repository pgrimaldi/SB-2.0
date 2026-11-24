import { DiscountScope } from '../../enums/discount-scope';
import { DiscountServiceCategory } from '../../enums/discount-service-category';
import { DiscountType } from '../../enums/discount-type';
import { VoucherValidityPeriod } from './voucher-validity-period';

export const MAX_VOUCHER_NAME_LENGTH = 120;
export const MAX_VOUCHER_CODE_LENGTH = 60;
export const MAX_VOUCHER_PERIOD_QUANTITY = 50;
export const MAX_VOUCHER_TIME_SLOT_QUANTITY = 20;
export const MAX_FIXED_DISCOUNT = 100_000;
export const MAX_VOUCHER_USE_QUANTITY = 2_147_483_647;

export class VoucherAddItemRequest {
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
