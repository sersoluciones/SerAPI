import { HttpParams } from '@angular/common/http';
import { hasValue, objHasValue } from '@sersol/ngx';

/**
 * Obtiene un arreglo correspondiente a la página, cantidad de filas, ordenamiento y filtro sobre los resultados en Rest
 * @param compThis Contexto del componente
 * @returns Devuelve un HttpParams con cada parametro
 */
export function getQueryParams(compThis: any): HttpParams {

    let params = new HttpParams();

    params = params.append('enable_pagination', 'true');
    params = params.append('page_size', `${compThis.paginationParams.pageSize}`);
    params = params.append('current_page', `${compThis.pagination.currentPage}`);

    if (hasValue(compThis.pagination.sortType)) {
        params = params.append('order_by', `${compThis.pagination.sortType} ${compThis.pagination.sortReverseSymbol}`);
    }

    if (objHasValue(compThis.filterFG.value)) {

        const filter_by = [];

        Object.keys(compThis.filterFG.controls).forEach(key => {

            if (hasValue(compThis.filterFG.controls[key].value)) {

                if (key !== 'all') {
                    filter_by.push(key + '¬' + compThis.filterFG.controls[key].value);
                }
            }

        });

        if (filter_by.length === 0) {
            if (hasValue(compThis.filterFG.controls.all.value)) {
                filter_by.push('$=' + compThis.filterFG.controls.all.value);
            }
        }

        if (filter_by.length > 0) {
            params = params.append('filter_by', filter_by.join('/'));
        }
    }

    return params;
}
