import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CrudFilterToolsComponent } from './crud-filter-tools.component';

describe('CrudFilterToolsComponent', () => {
  let component: CrudFilterToolsComponent;
  let fixture: ComponentFixture<CrudFilterToolsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CrudFilterToolsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CrudFilterToolsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
