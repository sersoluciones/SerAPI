import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DialogErrorBulkComponent } from './error-bulk.component';

describe('DialogErrorBulkComponent', () => {
  let component: DialogErrorBulkComponent;
  let fixture: ComponentFixture<DialogErrorBulkComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DialogErrorBulkComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DialogErrorBulkComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
