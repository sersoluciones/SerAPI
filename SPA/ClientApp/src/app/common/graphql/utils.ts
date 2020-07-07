import { FilterField } from './../interfaces/base';
import { hasValue, objHasValue } from '@sersol/ngx';


/**
 * Función para transformar un objeto en un string apto para filtrar resultados GraphQL
 * @param obj Objeto a convertir
 */
export function getGraphQLFilters(obj: any, filters: FilterField[]): string {

    let graph_params = '';

    for (const [key, value] of Object.entries(obj)) {
        if (hasValue(value)) {

            if (key !== 'all') {
                graph_params += key;

                const filter = filters.find(item => {
                    console.log(item.field, key);
                    return item.field === key;
                });

                if (filter.exact) {
                    graph_params += '_iext';
                }

                graph_params += ': ';
            } else {
                graph_params += key + ': ';
            }

            if (typeof (value) === 'string') {
                graph_params += '"' + value + '"';
            } else {
                graph_params += value;
            }

            graph_params += ',';
        }
    }

    if (hasValue(graph_params)) {
        graph_params = graph_params.substring(0, graph_params.length - 1);
    }

    return graph_params;
}

/**
 * Obtiene un arreglo correspondiente a la página, cantidad de filas, ordenamiento y filtro sobre los resultados en GraphQL
 * @returns Devuelve un arreglo de strings con cada parametro
 */
export function getGraphQueryParams(self): string[] {
    const params: string[] = [];

    params.push(`first: ${self.paginationParams.pageSize}`);
    params.push(`page: ${self.pagination.currentPage}`);

    if (hasValue(self.pagination.sortType)) {
        params.push(`orderBy: "${self.pagination.sortType} ${self.pagination.sortReverseSymbol}"`);
    }

    if (objHasValue(self.filterFG.value)) {
        params.push(getGraphQLFilters(self.filterFG.value, self.filters));
    }

    if (hasValue(self.graphQuery.list.filter)) {
        params.push(self.graphQuery.list.filter);
    }

    return params;
}
