import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
    name: 'filterArray',
    pure: false
})
export class FilterArray implements PipeTransform {

    transform(items: any[], filter: string, key: string): any {

        if (!items || !filter) {
            return items;
        }

        return items.filter(item => item[key].toLowerCase().indexOf(filter.toLowerCase()) !== -1);
    }

}
