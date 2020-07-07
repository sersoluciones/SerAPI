import { FormGroup } from '@angular/forms';

export interface PaginationParams {
    pageSize: number;
    orderBy?: string;
    sortReverse: boolean;
    filterBy?: string;
}

export interface GraphQuery {
    list?: {
        name: string;
        fields: string[];
        filter?: string;
    };
    delete?: {
        pk: any;
        name: string;
    };
    create?: {
        name: string;
    };
    update?: {
        name: string;
    };
}

export interface PaginationState {
    gettingMore: boolean;
    currentPage: number;
    rowCount: number;
    hasNextPage: boolean;
    activeTab?: number;
    searchQuery?: FormGroup;
    showFilters?: boolean;
    sortType?: string;
    sortReverse?: boolean;
    sortReverseSymbol?: string;
}

export interface FilterField {
    label: string;
    field: string;
    exact?: boolean;
    type: 'input' | 'select' | 'date';
}

export interface CrudPermissions {
    create?: string;
    update?: string;
    delete?: string;
}

export interface CrudState {
    formMode: boolean;
    isSaving: boolean;
    isDownloading: boolean;
    selectedObjects: number;
    texts?: any;
}

export type ExportSettingsFormats =  'xlsx' | 'csv';

export interface ExportSettings {
    formats: ExportSettingsFormats[];
    columns: string[];
    modelName: string;
}


export interface FormState {
    loading: boolean;
    message?: string;
    isEditing?: boolean;
    isSaving: boolean;
    isDownloading: boolean;
    maximized?: boolean;
    reloadOnclose: boolean;
    texts?: any;
    activeTab?: number;
}

export interface BaseFormData {
    instance?: any;
    maximized?: boolean;
}
