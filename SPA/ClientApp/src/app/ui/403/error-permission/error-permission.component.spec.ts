import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ErrorPermissionComponent } from './error-permission.component';

describe('ErrorPermissionComponent', () => {
  let component: ErrorPermissionComponent;
  let fixture: ComponentFixture<ErrorPermissionComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ErrorPermissionComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ErrorPermissionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
