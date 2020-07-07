import { hasValue } from '@sersol/ngx';

/**
 * @description
 * Interface que representa al objeto OidcUser usado para contener los datos del usuario
 */
export interface OidcUser {
    sub?: string;
    username?: string;
    name_initial?: string;
    name_to_show?: string;
    name?: string;
    last_name?: string;
    email?: string;
    email_verified?: boolean;
    phone_number_verified?: boolean;
    role?: string;
    claims?: string[];
    photo?: string;
    phone_number?: number;
    dark_mode?: boolean;
    is_super_user?: boolean;
    maximized_windows?: boolean;
    company_id?: number;
    company_name?: number;
}
