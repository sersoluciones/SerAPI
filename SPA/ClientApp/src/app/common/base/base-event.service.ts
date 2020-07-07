import { Injectable, Output, EventEmitter } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class BaseEventService {

    @Output() list: EventEmitter<void> = new EventEmitter();
    @Output() add: EventEmitter<void> = new EventEmitter();
    @Output() edit: EventEmitter<any> = new EventEmitter();
    @Output() saveAll: EventEmitter<void> = new EventEmitter();
    @Output() delete: EventEmitter<void> = new EventEmitter();
    @Output() download: EventEmitter<string> = new EventEmitter();

    getList() {
      this.list.emit();
    }

    addObject() {
      this.add.emit();
    }

    editObject(id: any) {
      this.edit.emit(id);
    }

    saveObjectList() {
      this.saveAll.emit();
    }

    downloadList(format: string) {
      this.download.emit(format);
    }
}
