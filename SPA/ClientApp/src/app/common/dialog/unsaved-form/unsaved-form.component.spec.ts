import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DialogUnsavedFormComponent } from './unsaved-form.component';

describe('UnsavedFormComponent', () => {
  let component: DialogUnsavedFormComponent;
  let fixture: ComponentFixture<DialogUnsavedFormComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DialogUnsavedFormComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DialogUnsavedFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
