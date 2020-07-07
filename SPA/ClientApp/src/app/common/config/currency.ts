import { CurrencyMaskInputMode } from 'ngx-currency';

export const customCurrencyMaskConfig = {
    align: 'right',
    allowNegative: false,
    allowZero: true,
    decimal: ',',
    precision: 0,
    prefix: '$ ',
    suffix: '',
    thousands: '.',
    nullable: true,
    min: null,
    max: null,
    inputMode: CurrencyMaskInputMode.NATURAL
};
