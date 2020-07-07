import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CrudAddObjectComponent } from './crud-add-object.component';

describe('CrudAddObjectComponent', () => {
  let component: CrudAddObjectComponent;
  let fixture: ComponentFixture<CrudAddObjectComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CrudAddObjectComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CrudAddObjectComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
