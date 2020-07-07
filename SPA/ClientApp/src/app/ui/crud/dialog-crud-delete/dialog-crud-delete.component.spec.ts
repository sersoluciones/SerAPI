import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DialogCrudDeleteComponent } from './dialog-crud-delete.component';

describe('DialogCrudDeleteComponent', () => {
  let component: DialogCrudDeleteComponent;
  let fixture: ComponentFixture<DialogCrudDeleteComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DialogCrudDeleteComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DialogCrudDeleteComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
