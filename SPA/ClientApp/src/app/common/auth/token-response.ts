export interface TokenResponse {
    decoded_access_token?: any;
    access_token: string;
    expires_in: number;
    token_type: string;
    refresh_token: string;
    scope: string;
}
